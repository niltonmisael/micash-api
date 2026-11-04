using CommonTestUtilities.Cache;
using CommonTestUtilities.Jobs;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Plans;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Time;
using CommonTestUtilities.Token;
using FluentAssertions;
using MiCash.Application.Jobs.Email;
using MiCash.Application.Jobs.Users;
using MiCash.Application.UseCases.Auth.EmailVerification;
using MiCash.Communication.Requests.EmailVerification;
using MiCash.Communication.Responses.EmailVerification;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Services.Email;
using MiCash.Domain.Services.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UseCases.Test.EmailVerification
{
    public class EmailVerificationUseCasesTest
    {
        private const string EMAIL = "joana@micash.com.br";

        private readonly User _user = new()
        {
            Id = 11 ,
            Name = "Joana Lima" ,
            Email = EMAIL ,
            PasswordHash = "hash:Senha1!" ,
            UserIdentifier = Guid.NewGuid() ,
            EmailVerifiedAt = null
        };

        private readonly InMemoryEmailVerificationRepository _repository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IUserReadOnlyRepository> _userReads = new();
        private readonly Mock<IUserUpdateOnlyRepository> _userUpdates = new();
        private readonly FakeBackgroundJobService _jobs = new();
        private readonly FakeRedisCacheService _cache = new();
        private readonly FakeEncripter _encripter = new();
        private readonly Mock<ITransactionalEmailService> _emails = new();
        private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026 , 11 , 4 , 12 , 0 , 0 , TimeSpan.Zero));
        private readonly List<(string To, EmailVerificationEmail Email)> _sentCodes = [];
        private readonly List<(string To, TransactionalEmail Email)> _sent = [];

        public EmailVerificationUseCasesTest ()
        {
            _repository.Users[_user.Id] = _user;
            _userUpdates.Setup(u => u.GetById(_user.Id)).ReturnsAsync(_user);

            _emails.Setup(e => e.SendAsync(It.IsAny<string>() , It.IsAny<TransactionalEmail>() , It.IsAny<string?>() , It.IsAny<CancellationToken>()))
                .Callback<string , TransactionalEmail , string? , CancellationToken>(( to , email , _ , _ ) => {
                    _sent.Add((to, email));
                    if ( email is EmailVerificationEmail verification ) _sentCodes.Add((to, verification));
                })
                .ReturnsAsync("email-id");
        }

        // ---------------- Verify ----------------

        [Fact]
        public async Task Right_Code_Verifies_The_Account_Returns_New_Tokens_And_Queues_The_Welcome_Email ()
        {
            var code = await CodeSent();

            var response = await Verify(code);

            _user.EmailVerifiedAt.Should().Be(_time.UtcNow);
            response.EmailVerified.Should().BeTrue();
            response.Token.Should().NotBeNullOrWhiteSpace();
            response.RefreshToken.Should().NotBeNullOrWhiteSpace();
            response.Name.Should().Be(_user.Name);
            _repository.Requests.Last().VerifiedAt.Should().Be(_time.UtcNow);
            _jobs.EnqueuedWelcomeEmails.Should().Equal(_user.Id);
        }

        [Fact]
        public async Task Free_User_Gets_The_Premium_Offer_And_A_Subscriber_Does_Not ()
        {
            var code = await CodeSent();
            (await Verify(code , UserPlanServiceBuilder.Free())).ShowPremiumOffer.Should().BeTrue();

            (await Verify("000000" , UserPlanServiceBuilder.Build(PlanLimits.Premium))).ShowPremiumOffer.Should().BeFalse();
        }

        [Fact]
        public async Task Wrong_Code_Is_Refused_And_Five_Wrong_Codes_Invalidate_It ()
        {
            var code = await CodeSent();

            for ( var i = 0; i < EmailVerificationRules.MAX_ATTEMPTS; i++ ) {
                await ExpectInvalidCode(() => Verify(Other(code)));
            }

            _repository.Requests.Last().InvalidatedAt.Should().NotBeNull();
            await ExpectInvalidCode(() => Verify(code));
            _user.EmailVerifiedAt.Should().BeNull();
        }

        [Fact]
        public async Task Expired_Code_Is_Refused ()
        {
            var code = await CodeSent();
            _time.Advance(EmailVerificationRules.CodeValidity);

            await ExpectInvalidCode(() => Verify(code));
            _user.EmailVerifiedAt.Should().BeNull();
        }

        [Fact]
        public async Task Code_Sent_To_The_Previous_Address_Does_Not_Verify_The_Corrected_One ()
        {
            var code = await CodeSent();
            _user.Email = "outro@micash.com.br";

            await ExpectInvalidCode(() => Verify(code));
            _user.EmailVerifiedAt.Should().BeNull();
        }

        [Fact]
        public async Task Without_A_Code_Sent_The_Verification_Fails_With_The_Same_Message ()
        {
            await ExpectInvalidCode(() => Verify("123456"));
        }

        [Fact]
        public async Task Invalid_Code_Format_Is_Refused ()
        {
            var act = () => Verify("12a");

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.PASSWORD_RESET_CODE_FORMAT);
        }

        [Fact]
        public async Task Verifying_Again_After_The_Confirmation_Just_Returns_Tokens ()
        {
            var code = await CodeSent();
            await Verify(code);
            var verifiedAt = _user.EmailVerifiedAt;

            var again = await Verify("999999");

            again.EmailVerified.Should().BeTrue();
            _user.EmailVerifiedAt.Should().Be(verifiedAt);
            _jobs.EnqueuedWelcomeEmails.Should().ContainSingle("the welcome e-mail is sent only once");
        }

        // ---------------- Resend ----------------

        [Fact]
        public async Task Resend_Replaces_The_Previous_Code ()
        {
            var first = await CodeSent();
            _time.Advance(EmailVerificationRules.ResendInterval);

            var response = await Resend();
            await RunEmailJob();

            response.Should().BeEquivalentTo(EmailVerificationRules.CodeSent(EMAIL));
            _repository.Requests.First().InvalidatedAt.Should().NotBeNull();
            _jobs.EnqueuedEmailVerificationEmails.Should().HaveCount(2);

            await Verify(_sentCodes.Last().Email.Code);
            _user.EmailVerifiedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Resend_Within_The_Interval_Is_429_With_The_Release_Time ()
        {
            await CodeSent();
            _time.Advance(TimeSpan.FromSeconds(20));

            var act = () => Resend();

            var error = (await act.Should().ThrowAsync<TooManyRequestsException>()).Which;
            error.RetryAt.Should().Be(_repository.Requests.Last().CreatedAt + EmailVerificationRules.ResendInterval);
            error.GetErrors().Single().Should().Contain("40");
            _repository.Requests.Should().ContainSingle();
        }

        [Fact]
        public async Task More_Than_Five_Codes_Per_Hour_Are_Refused ()
        {
            for ( var i = 0; i < EmailVerificationRules.MAX_REQUESTS_PER_HOUR; i++ ) {
                await Resend();
                _time.Advance(EmailVerificationRules.ResendInterval);
            }

            await FluentActions.Invoking(() => Resend()).Should().ThrowAsync<TooManyRequestsException>();
            _repository.Requests.Should().HaveCount(EmailVerificationRules.MAX_REQUESTS_PER_HOUR);
        }

        [Fact]
        public async Task Verified_Account_Cannot_Ask_For_A_Code ()
        {
            _user.EmailVerifiedAt = _time.UtcNow;

            (await FluentActions.Invoking(() => Resend()).Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.EMAIL_ALREADY_VERIFIED);
        }

        // ---------------- Change e-mail ----------------

        [Fact]
        public async Task Correcting_The_Email_Sends_A_Code_To_The_New_Address ()
        {
            await CodeSent();
            _time.Advance(EmailVerificationRules.ResendInterval);

            var response = await ChangeEmail("  joana.certo@micash.com.br ");
            await RunEmailJob();

            response.Email.Should().Be("joana.certo@micash.com.br");
            _user.Email.Should().Be("joana.certo@micash.com.br");
            _repository.Requests.First().InvalidatedAt.Should().NotBeNull();
            _sentCodes.Last().To.Should().Be("joana.certo@micash.com.br");

            await Verify(_sentCodes.Last().Email.Code);
            _user.EmailVerifiedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Correcting_To_An_Email_Already_Registered_Is_Refused ()
        {
            _userReads.Setup(u => u.ExistActiveUserWithEmail("dono@micash.com.br")).ReturnsAsync(true);

            (await FluentActions.Invoking(() => ChangeEmail("dono@micash.com.br")).Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.EMAIL_ALREADY_REGISTERED);
            _user.Email.Should().Be(EMAIL);
        }

        [Fact]
        public async Task Correcting_The_Email_Respects_The_Resend_Interval ()
        {
            await CodeSent();

            await FluentActions.Invoking(() => ChangeEmail("joana.certo@micash.com.br")).Should().ThrowAsync<TooManyRequestsException>();
            _user.Email.Should().Be(EMAIL);
        }

        [Fact]
        public async Task Invalid_Email_Is_Refused ()
        {
            (await FluentActions.Invoking(() => ChangeEmail("nao-e-email")).Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.EMAIL_INVALID);
        }

        // ---------------- Job ----------------

        [Fact]
        public async Task Job_Sends_The_Code_To_The_Request_Address_And_Stores_Only_The_Hash ()
        {
            await Resend();
            await RunEmailJob();

            var (to, email) = _sentCodes.Should().ContainSingle().Subject;
            to.Should().Be(EMAIL);
            email.Name.Should().Be(_user.Name);
            email.Code.Should().MatchRegex(@"^\d{6}$");
            email.ValidityMinutes.Should().Be(15);
            _repository.Requests.Single().CodeHash.Should().Be($"hash:{email.Code}");
        }

        [Fact]
        public async Task Job_Skips_Requests_Of_Verified_Accounts_And_Corrected_Addresses ()
        {
            await Resend();
            _user.EmailVerifiedAt = _time.UtcNow;
            await RunEmailJob();

            _user.EmailVerifiedAt = null;
            _user.Email = "corrigido@micash.com.br";
            await RunEmailJob();

            _sentCodes.Should().BeEmpty();
        }

        // ---------------- E-mail change (verified account) ----------------

        private const string NEW_EMAIL = "joana.nova@micash.com.br";
        private const string PASSWORD = "Senha1!";

        [Fact]
        public async Task Email_Change_Sends_The_Code_To_The_New_Address_And_Keeps_The_Current_Email ()
        {
            _user.EmailVerifiedAt = _time.UtcNow;

            var response = await RequestChange($"  {NEW_EMAIL} ");
            await RunEmailJob();

            response.Should().BeEquivalentTo(EmailVerificationRules.CodeSent(NEW_EMAIL));
            _user.Email.Should().Be(EMAIL , "the account changes only after the code");
            var request = _repository.Requests.Single();
            request.Purpose.Should().Be(EmailVerificationPurpose.EmailChange);
            request.Email.Should().Be(NEW_EMAIL);

            var (to, email) = _sent.Should().ContainSingle().Subject;
            to.Should().Be(NEW_EMAIL);
            email.Should().BeOfType<EmailChangeConfirmationEmail>();
            request.CodeHash.Should().Be($"hash:{((EmailChangeConfirmationEmail)email).Code}");
        }

        [Fact]
        public async Task Email_Change_Requires_The_Current_Password ()
        {
            _user.EmailVerifiedAt = _time.UtcNow;

            (await FluentActions.Invoking(() => RequestChange(NEW_EMAIL , "Errada1!")).Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.PASSWORD_DIFFERENT_CURRENT_PASSWORD);
            _repository.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task Email_Change_To_The_Same_Or_A_Registered_Address_Is_Refused ()
        {
            _user.EmailVerifiedAt = _time.UtcNow;
            _userReads.Setup(u => u.ExistActiveUserWithEmail("dono@micash.com.br")).ReturnsAsync(true);

            (await FluentActions.Invoking(() => RequestChange(EMAIL)).Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.EMAIL_CHANGE_SAME_AS_CURRENT);
            (await FluentActions.Invoking(() => RequestChange("dono@micash.com.br")).Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.EMAIL_ALREADY_REGISTERED);
            _repository.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task Confirming_The_Change_Moves_The_Account_And_Notifies_The_Previous_Address ()
        {
            var code = await ChangeCodeSent();

            var profile = await ConfirmChange(code);

            _user.Email.Should().Be(NEW_EMAIL);
            _user.EmailVerifiedAt.Should().Be(_time.UtcNow);
            profile.Email.Should().Be(NEW_EMAIL);
            profile.EmailVerified.Should().BeTrue();
            var request = _repository.Requests.Single();
            request.VerifiedAt.Should().Be(_time.UtcNow);
            _jobs.EnqueuedEmailChangedNotices.Should().Equal((request.Id, EMAIL));

            // The notice job sends template 07 to the previous address.
            await new EmailChangedNoticeJob(_repository , _emails.Object , NullLogger<EmailChangedNoticeJob>.Instance)
                .ExecuteAsync(request.Id , EMAIL , CancellationToken.None);

            var (to, notice) = _sent.Last();
            to.Should().Be(EMAIL);
            notice.Should().BeEquivalentTo(new EmailChangedNoticeEmail(_user.Name , NEW_EMAIL));
        }

        [Fact]
        public async Task Wrong_Or_Missing_Change_Code_Changes_Nothing ()
        {
            _user.EmailVerifiedAt = _time.UtcNow;
            await ExpectInvalidCode(() => ConfirmChange("123456"));

            var code = await ChangeCodeSent();
            await ExpectInvalidCode(() => ConfirmChange(Other(code)));

            _user.Email.Should().Be(EMAIL);
            _jobs.EnqueuedEmailChangedNotices.Should().BeEmpty();
        }

        [Fact]
        public async Task Change_Code_Cannot_Be_Used_Twice ()
        {
            var code = await ChangeCodeSent();
            await ConfirmChange(code);

            await ExpectInvalidCode(() => ConfirmChange(code));
        }

        [Fact]
        public async Task Address_Registered_By_Someone_Else_Meanwhile_Is_Refused_At_Confirmation ()
        {
            var code = await ChangeCodeSent();
            _userReads.Setup(u => u.ExistActiveUserWithEmail(NEW_EMAIL)).ReturnsAsync(true);

            (await FluentActions.Invoking(() => ConfirmChange(code)).Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.EMAIL_ALREADY_REGISTERED);
            _user.Email.Should().Be(EMAIL);
            _repository.Requests.Single().InvalidatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Sign_Up_Code_Does_Not_Confirm_An_Email_Change ()
        {
            var signUpCode = await CodeSent();
            _user.EmailVerifiedAt = _time.UtcNow;

            await ExpectInvalidCode(() => ConfirmChange(signUpCode));
        }

        // ---------------- Cleanup ----------------

        [Fact]
        public async Task Cleanup_Removes_Unverified_Accounts_Older_Than_Seven_Days_In_Batches ()
        {
            var users = new Mock<IUserWriteOnlyRepository>();
            users.SetupSequence(u => u.RemoveUnverifiedCreatedBeforeAsync(It.IsAny<DateTime>() , It.IsAny<int>() , It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnverifiedAccountCleanupJob.BATCH_SIZE)
                .ReturnsAsync(3);

            await new UnverifiedAccountCleanupJob(users.Object , _unitOfWork.Object , _time , NullLogger<UnverifiedAccountCleanupJob>.Instance)
                .ExecuteAsync(CancellationToken.None);

            users.Verify(u => u.RemoveUnverifiedCreatedBeforeAsync(
                _time.UtcNow - EmailVerificationRules.UnverifiedAccountLifetime , UnverifiedAccountCleanupJob.BATCH_SIZE , It.IsAny<CancellationToken>()) , Times.Exactly(2));
            _unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Exactly(2));
        }

        // ---------------- helpers ----------------

        private EmailVerificationCodeIssuer Issuer () =>
            new(_repository , _repository , _repository , _jobs , NullLogger<EmailVerificationCodeIssuer>.Instance);

        private EmailVerificationCodeChecker Checker () =>
            new(_repository , _unitOfWork.Object , _encripter , NullLogger<EmailVerificationCodeChecker>.Instance);

        private Task<ResponseEmailVerifiedJson> Verify ( string code , IUserPlanService? plans = null ) =>
            new VerifyEmailUseCase(
                LoggedUserBuilder.Build(_user) , _userUpdates.Object , Checker() , Issuer() ,
                JwtTokenGeneratorBuilder.Build() , RefreshTokenGeneratorBuilder.Build() , _cache ,
                plans ?? UserPlanServiceBuilder.Free() , _jobs , _time , NullLogger<VerifyEmailUseCase>.Instance)
            .Execute(new RequestVerifyEmailJson { Code = code } , CancellationToken.None);

        private Task<ResponseEmailVerificationCodeSentJson> Resend () =>
            new ResendEmailVerificationCodeUseCase(
                LoggedUserBuilder.Build(_user) , Issuer() , _unitOfWork.Object , _time ,
                NullLogger<ResendEmailVerificationCodeUseCase>.Instance)
            .Execute(CancellationToken.None);

        private Task<ResponseEmailVerificationCodeSentJson> ChangeEmail ( string email ) =>
            new ChangeVerificationEmailUseCase(
                LoggedUserBuilder.Build(_user) , _userReads.Object , _userUpdates.Object , Issuer() , _unitOfWork.Object , _time ,
                NullLogger<ChangeVerificationEmailUseCase>.Instance)
            .Execute(new RequestChangeVerificationEmailJson { Email = email } , CancellationToken.None);

        private Task<ResponseEmailVerificationCodeSentJson> RequestChange ( string newEmail , string password = PASSWORD ) =>
            new RequestEmailChangeUseCase(
                LoggedUserBuilder.Build(_user) , _userReads.Object , _encripter , Issuer() , _unitOfWork.Object , _time ,
                NullLogger<RequestEmailChangeUseCase>.Instance)
            .Execute(new RequestEmailChangeJson { NewEmail = newEmail , Password = password } , CancellationToken.None);

        private Task<MiCash.Communication.Responses.ResponseUserProfileJson> ConfirmChange ( string code ) =>
            new ConfirmEmailChangeUseCase(
                LoggedUserBuilder.Build(_user) , _userReads.Object , _userUpdates.Object , Checker() , Issuer() , _jobs , _time ,
                NullLogger<ConfirmEmailChangeUseCase>.Instance)
            .Execute(new RequestConfirmEmailChangeJson { Code = code } , CancellationToken.None);

        private async Task<string> ChangeCodeSent ()
        {
            _user.EmailVerifiedAt = _time.UtcNow;
            await RequestChange(NEW_EMAIL);
            await RunEmailJob();
            return ((EmailChangeConfirmationEmail)_sent.Last().Email).Code;
        }

        private Task RunEmailJob () =>
            new EmailVerificationEmailJob(_repository , _unitOfWork.Object , _encripter , _emails.Object , _time ,
                NullLogger<EmailVerificationEmailJob>.Instance)
            .ExecuteAsync(_repository.Requests.Last().Id , CancellationToken.None);

        private async Task<string> CodeSent ()
        {
            await Resend();
            await RunEmailJob();
            return _sentCodes.Last().Email.Code;
        }

        private static async Task ExpectInvalidCode ( Func<Task> act ) =>
            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.EMAIL_VERIFICATION_CODE_INVALID);

        private static string Other ( string code ) => code == "000000" ? "111111" : "000000";

        /// <summary>Deterministic stand-in for BCrypt: "hash:" + value.</summary>
        private sealed class FakeEncripter : IPasswordEncripter
        {
            public string Encrypt ( string password ) => $"hash:{password}";

            public bool Verify ( string password , string passwordHash ) => passwordHash == $"hash:{password}";
        }
    }
}
