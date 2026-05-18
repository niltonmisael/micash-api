using CommonTestUtilities.Cache;
using CommonTestUtilities.Cryptography;
using CommonTestUtilities.Entities;
using CommonTestUtilities.Logger;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using CommonTestUtilities.Token;
using FluentAssertions;
using MiCash.Application.UseCases.Auth.Login;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;
using Moq;

namespace UseCases.Test.Login.DoLogin
{
    public class DoLoginUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var user = UserBuilder.Build();

            var request = RequestLoginJsonBuilder.Build();
            request.Email = user.Email;

            var useCase = CreateUseCase(user , request.Password);

            var result = await useCase.Execute(request);

            result.Should().NotBeNull();
            result.Name.Should().Be(user.Name);
            result.Token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Error_User_Not_Found ()
        {
            var user = UserBuilder.Build();

            var request = RequestLoginJsonBuilder.Build();
            var useCase = CreateUseCase(user);

            var act = async () => await useCase.Execute(request);

            var result = await act.Should().ThrowAsync<InvalidLoginException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourceErrorMessages.EMAIL_OR_PASSWORD_INVALID));
        }

        [Fact]
        public async Task Error_Password_Not_Match ()
        {
            var user = UserBuilder.Build();

            var request = RequestLoginJsonBuilder.Build();
            var useCase = CreateUseCase(user);

            var act = async () => await useCase.Execute(request);

            var result = await act.Should().ThrowAsync<InvalidLoginException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourceErrorMessages.EMAIL_OR_PASSWORD_INVALID));
        }

        private DoLoginUseCase CreateUseCase ( MiCash.Domain.Entities.User user, string? password = null )
        {
            var passwordEncripter = new PasswordEncrypterBuilder().Verify(password).Build();
            var tokenGenerator = JwtTokenGeneratorBuilder.Build();
            var readRepository = new UserReadOnlyRepositoryBuilder().GetUserByEmail(user).Build();
            var refreshTokenGenerator = RefreshTokenGeneratorBuilder.Build();
            var redisCacheService = new RedisCacheServiceBuilder().Build();
            var logger = new LoggerServiceBuilder<DoLoginUseCase>().Build();

            return new DoLoginUseCase(
                readRepository, 
                passwordEncripter , 
                tokenGenerator,
                refreshTokenGenerator,
                redisCacheService,
                logger);
        }
    }
}
