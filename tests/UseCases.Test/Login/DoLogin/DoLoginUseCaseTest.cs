using CommonTestUtilities.Cryptography;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Token;
using FluentAssertions;
using MiCash.Application.UseCases.Login;
using MiCash.Exception.ExceptionBase;
using MiCash.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTestUtilities.Entities;
using CommonTestUtilities.Requests;

namespace UseCases.Test.Login.DoLogin
{
    public class DoLoginUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var user = UserBuilder.Build();

            var request = RequestLoginJsonBuilder.Build();

            var useCase = CreateUseCase(user);

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

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourseErrorMessages.EMAIL_OR_PASSWORD_INVALID));
        }

        [Fact]
        public async Task Error_Password_Not_Match ()
        {
            var user = UserBuilder.Build();

            var request = RequestLoginJsonBuilder.Build();
            var useCase = CreateUseCase(user);

            var act = async () => await useCase.Execute(request);

            var result = await act.Should().ThrowAsync<ErrorOnValidationException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourseErrorMessages.EMAIL_OR_PASSWORD_INVALID));
        }

        private DoLoginUseCase CreateUseCase ( MiCash.Domain.Entities.User user )
        {
            var passwordEncripter = new PasswordEncrypterBuilder().Build();
            var tokenGenerator = JwtTokenGeneratorBuilder.Build();
            var readRepository = new UserReadOnlyRepositoryBuilder().GetUserByEmail(user).Build();

            return new DoLoginUseCase(readRepository, passwordEncripter , tokenGenerator);
        }
    }
}
