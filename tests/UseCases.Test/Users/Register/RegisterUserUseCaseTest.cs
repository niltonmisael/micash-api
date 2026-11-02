using CommonTestUtilities.Cache;
using CommonTestUtilities.Cryptography;
using CommonTestUtilities.Jobs;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using CommonTestUtilities.Token;
using FluentAssertions;
using MiCash.Application.UseCases.Users.Register;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging.Abstractions;

namespace UseCases.Test.Users.Register
{
    public class RegisterUserUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var cancellationToken = CancellationToken.None;
            var request = RequestRegisterUserJsonBuilder.Build();
            var useCase = CreateUseCase();

            var result = await useCase.Execute(request, cancellationToken);

            result.Should().NotBeNull();
            result.Name.Should().Be(request.Name);
            result.RefreshToken.Should().NotBeNullOrWhiteSpace();
            result.Token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Error_Name_Empty ()
        {
            var cancellationToken = CancellationToken.None;
            var request = RequestRegisterUserJsonBuilder.Build();
            request.Name = string.Empty;

            var useCase = CreateUseCase();

            var act = async () => await useCase.Execute(request, cancellationToken);

            var result = await act.Should().ThrowAsync<ErrorOnValidationException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourceErrorMessages.NAME_EMPTY));
        }

        [Fact]
        public async Task Error_Email_Already_Exist ()
        {

            var cancellationToken = CancellationToken.None;
            var request = RequestRegisterUserJsonBuilder.Build();

            var useCase = CreateUseCase(request.Email);

            var act = async () => await useCase.Execute(request, cancellationToken);

            var result = await act.Should().ThrowAsync<ErrorOnValidationException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourceErrorMessages.EMAIL_ALREADY_REGISTERED));
        }

        [Fact]
        public async Task Welcome_Email_Is_Queued_After_The_Account_Is_Saved ()
        {
            var jobs = new FakeBackgroundJobService();

            await CreateUseCase(jobs: jobs).Execute(RequestRegisterUserJsonBuilder.Build() , CancellationToken.None);

            jobs.EnqueuedWelcomeEmails.Should().ContainSingle();
        }

        [Fact]
        public async Task Registration_Succeeds_Even_If_The_Welcome_Email_Cannot_Be_Queued ()
        {
            var jobs = new FakeBackgroundJobService { ThrowOnEnqueue = true };
            var request = RequestRegisterUserJsonBuilder.Build();

            var result = await CreateUseCase(jobs: jobs).Execute(request , CancellationToken.None);

            result.Name.Should().Be(request.Name);
            result.Token.Should().NotBeNullOrWhiteSpace();
            jobs.EnqueuedWelcomeEmails.Should().BeEmpty();
        }

        [Fact]
        public async Task Invalid_Registration_Does_Not_Queue_The_Welcome_Email ()
        {
            var jobs = new FakeBackgroundJobService();
            var request = RequestRegisterUserJsonBuilder.Build();
            request.Name = string.Empty;

            var act = () => CreateUseCase(jobs: jobs).Execute(request , CancellationToken.None);

            await act.Should().ThrowAsync<ErrorOnValidationException>();
            jobs.EnqueuedWelcomeEmails.Should().BeEmpty();
        }

        private RegisterUserUseCase CreateUseCase ( string? email = null , FakeBackgroundJobService? jobs = null )
        {
            var mapper = MapperBuilder.Build();
            var passwordEncripter = new PasswordEncrypterBuilder().Build();
            var writeRepository = UserWriteOnlyRepositoryBuilder.Build();
            var unitOfWork = UnitOfWorkBuilder.Build();
            var tokenGenerator = JwtTokenGeneratorBuilder.Build();
            var readRepository = new UserReadOnlyRepositoryBuilder();
            var refreshTokenGenerator = RefreshTokenGeneratorBuilder.Build();
            var redisCacheService = new RedisCacheServiceBuilder();

            if ( string.IsNullOrWhiteSpace(email) == false) {
                readRepository.ExistActiveUserWithEmail(email);
            }

            return new RegisterUserUseCase(
                mapper, 
                passwordEncripter, 
                readRepository.Build(), 
                writeRepository , 
                unitOfWork , 
                tokenGenerator,
                refreshTokenGenerator,
                redisCacheService.Build() ,
                jobs ?? new FakeBackgroundJobService() ,
                NullLogger<RegisterUserUseCase>.Instance
                );

        }

    }
}
