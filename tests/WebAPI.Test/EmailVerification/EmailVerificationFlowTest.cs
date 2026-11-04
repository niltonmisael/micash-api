using FluentAssertions;
using MiCash.Communication.Requests;
using MiCash.Communication.Requests.EmailVerification;
using MiCash.Domain.Services.Jobs;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebAPI.Test.EmailVerification
{
    /// <summary>
    /// Sign-up over HTTP: register → blocked (403 EMAIL_NOT_VERIFIED) → (Hangfire job, run here by hand) → code
    /// e-mail → verify → new tokens unlock the API. E-mails go to the factory's fake; nothing is sent.
    /// </summary>
    public class EmailVerificationFlowTest : MiCashClassFixture
    {
        private const string PASSWORD = "Senha123!";

        private readonly CustomWebApplicationFactory _factory;

        public EmailVerificationFlowTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Register_Is_Blocked_Until_The_Code_Is_Confirmed ()
        {
            var email = $"novo.{Guid.NewGuid():N}@micash.com.br";

            // 1. Registration: session tokens, but not verified; the code is queued (not the welcome e-mail).
            var register = await DoPost("api/user/register" , NewUser(email));
            register.StatusCode.Should().Be(HttpStatusCode.Created);
            var registered = await ReadJson(register);
            registered.GetProperty("emailVerified").GetBoolean().Should().BeFalse();
            var unverifiedToken = registered.GetProperty("token").GetString()!;

            // 2. Everything but the verification endpoints answers 403 EMAIL_NOT_VERIFIED.
            var blocked = await DoGet("api/subscription/limits" , unverifiedToken);
            blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var error = await ReadJson(blocked);
            error.GetProperty("errorCode").GetString().Should().Be("EMAIL_NOT_VERIFIED");
            error.GetProperty("errorMessages")[0].GetString()
                .Should().Be("Confirme seu e-mail para continuar. Digite o código que enviamos para o seu endereço.");

            var profile = await DoGet("api/user/profile" , unverifiedToken);
            profile.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadJson(profile)).GetProperty("emailVerified").GetBoolean().Should().BeFalse();

            // Login before confirming works and tells the app to open the code screen.
            var login = await DoPost("login" , new RequestLoginJson { Email = email , Password = PASSWORD });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadJson(login)).GetProperty("emailVerified").GetBoolean().Should().BeFalse();

            // 3. The job generates the code and sends template 01.
            var code = await RunJobAndReadCode(email);

            var wrong = await DoPost("verify-email" , new RequestVerifyEmailJson { Code = code == "000000" ? "111111" : "000000" } , unverifiedToken);
            wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(wrong)).GetProperty("errorMessages")[0].GetString()
                .Should().Be("Código inválido ou expirado. Confira o código ou solicite um novo.");

            // 4. Right code: new tokens, verified, Premium offer for a Free user, welcome e-mail queued.
            var verify = await DoPost("verify-email" , new RequestVerifyEmailJson { Code = code } , unverifiedToken);
            verify.StatusCode.Should().Be(HttpStatusCode.OK);
            var verified = await ReadJson(verify);
            verified.GetProperty("emailVerified").GetBoolean().Should().BeTrue();
            verified.GetProperty("showPremiumOffer").GetBoolean().Should().BeTrue();
            verified.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
            var verifiedToken = verified.GetProperty("token").GetString()!;

            _factory.BackgroundJobs.EnqueuedWelcomeEmails.Should().NotBeEmpty();

            // 5. The new token unlocks the API; the old one still carries "not verified" until it expires.
            (await DoGet("api/subscription/limits" , verifiedToken)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await DoGet("api/subscription/limits" , unverifiedToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var loginAfter = await DoPost("login" , new RequestLoginJson { Email = email , Password = PASSWORD });
            (await ReadJson(loginAfter)).GetProperty("emailVerified").GetBoolean().Should().BeTrue();

            // Resending after the confirmation makes no sense.
            (await DoPost("resend-email-verification-code" , new { } , verifiedToken))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Resend_Waits_The_Interval_And_The_Email_Can_Be_Corrected ()
        {
            var email = $"errado.{Guid.NewGuid():N}@micash.com.br";

            var registered = await ReadJson(await DoPost("api/user/register" , NewUser(email)));
            var token = registered.GetProperty("token").GetString()!;

            // The registration just sent a code: a resend right away is 429 with retryAt.
            var resend = await DoPost("resend-email-verification-code" , new { } , token);
            resend.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            (await ReadJson(resend)).TryGetProperty("retryAt" , out _).Should().BeTrue();

            var correction = await DoPut("email-verification/email" , new RequestChangeVerificationEmailJson { Email = "x@micash.com.br" } , token);
            correction.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

            // An address already registered is refused (validation comes before the interval).
            var taken = await DoPut("email-verification/email" , new RequestChangeVerificationEmailJson { Email = _factory.GetEmail() } , token);
            taken.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Verified_User_Changes_The_Email_Only_After_The_Code_Sent_To_The_New_Address ()
        {
            var email = $"troca.{Guid.NewGuid():N}@micash.com.br";
            var newEmail = $"nova.{Guid.NewGuid():N}@micash.com.br";

            // A verified account.
            var registered = await ReadJson(await DoPost("api/user/register" , NewUser(email)));
            var code = await RunJobAndReadCode(email);
            var verified = await ReadJson(await DoPost("verify-email" , new RequestVerifyEmailJson { Code = code } , registered.GetProperty("token").GetString()!));
            var token = verified.GetProperty("token").GetString()!;

            // The profile update no longer changes the e-mail by itself.
            var direct = await DoPut("api/user/update" , new RequestUpdateUserJson { Name = "Pessoa Nova" , Email = newEmail } , token);
            direct.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(direct)).GetProperty("errorCode").GetString().Should().Be("EMAIL_CHANGE_REQUIRES_CONFIRMATION");

            // Wrong password: refused.
            (await DoPost("api/user/email-change" , new RequestEmailChangeJson { NewEmail = newEmail , Password = "Errada123!" } , token))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Step 1: code to the new address; the account keeps the current e-mail meanwhile.
            var change = await DoPost("api/user/email-change" , new RequestEmailChangeJson { NewEmail = newEmail , Password = PASSWORD } , token);
            change.StatusCode.Should().Be(HttpStatusCode.Accepted);
            (await ReadJson(change)).GetProperty("email").GetString().Should().Be(newEmail);
            (await DoPost("login" , new RequestLoginJson { Email = email , Password = PASSWORD })).StatusCode.Should().Be(HttpStatusCode.OK);

            var changeCode = await RunJobAndReadCode(newEmail , "Confirme seu novo e-mail · MiCash");

            // Step 2: confirmed → the account moves to the new address; a notice goes to the previous one.
            var confirm = await DoPost("api/user/email-change/confirm" , new RequestConfirmEmailChangeJson { Code = changeCode } , token);
            confirm.StatusCode.Should().Be(HttpStatusCode.OK);
            var profile = await ReadJson(confirm);
            profile.GetProperty("email").GetString().Should().Be(newEmail);
            profile.GetProperty("emailVerified").GetBoolean().Should().BeTrue();

            _factory.BackgroundJobs.EnqueuedEmailChangedNotices.Should().Contain(n => n.PreviousEmail == email);

            (await DoPost("login" , new RequestLoginJson { Email = newEmail , Password = PASSWORD })).StatusCode.Should().Be(HttpStatusCode.OK);
            (await DoPost("login" , new RequestLoginJson { Email = email , Password = PASSWORD })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Existing_Verified_Users_Are_Not_Affected ()
        {
            (await DoGet("api/subscription/limits" , _factory.GetToken())).StatusCode.Should().Be(HttpStatusCode.OK);

            var login = await DoPost("login" , new RequestLoginJson { Email = _factory.GetEmail() , Password = _factory.GetPassword() });
            (await ReadJson(login)).GetProperty("emailVerified").GetBoolean().Should().BeTrue();
        }

        private async Task<string> RunJobAndReadCode ( string email , string subject = "Confirme seu e-mail · MiCash" )
        {
            var requestId = _factory.BackgroundJobs.EnqueuedEmailVerificationEmails.Last();

            await using ( var scope = _factory.Services.CreateAsyncScope() ) {
                await scope.ServiceProvider.GetRequiredService<IEmailVerificationEmailJob>().ExecuteAsync(requestId , CancellationToken.None);
            }

            var sent = _factory.Emails.Sent.Should().ContainSingle(m => m.To == email).Subject;
            sent.Subject.Should().Be(subject);
            var code = Regex.Match(sent.TextBody! , @"Seu código: (\d{6})").Groups[1].Value;
            code.Should().HaveLength(6);

            return code;
        }

        private static RequestRegisterUserJson NewUser ( string email ) => new()
        {
            Name = "Pessoa Nova" ,
            Email = email ,
            Password = PASSWORD ,
            Phone = "43977777777"
        };

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
