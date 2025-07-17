using CommonTestUtilities.Cryptography;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using CommonTestUtilities.Token;
using FluentAssertions;
using MiCash.Application.UseCases.Users.Register;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.Test.Users.Register
{
    public class RegisterUserUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var request = RequestRegisterUserJsonBuilder.Build();
            var useCase = CreateUseCase();

            var result = await useCase.Execute(request);

            result.Should().NotBeNull();
            result.Name.Should().Be(request.Name);
            result.Token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Error_Name_Empty ()
        {
            var request = RequestRegisterUserJsonBuilder.Build();
            request.Name = string.Empty;

            var useCase = CreateUseCase();

            var act = async () => await useCase.Execute(request);

            var result = await act.Should().ThrowAsync<ErrorOnValidationException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourseErrorMessages.NAME_EMPTY));
        }

        [Fact]
        public async Task Error_Email_Already_Exist ()
        {
            var request = RequestRegisterUserJsonBuilder.Build();

            var useCase = CreateUseCase(request.Email);

            var act = async () => await useCase.Execute(request);

            var result = await act.Should().ThrowAsync<ErrorOnValidationException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourseErrorMessages.EMAIL_ALREADY_REGISTERED));
        }

        private RegisterUserUseCase CreateUseCase ( string? email = null)
        {
            var mapper = MapperBuilder.Build();
            var passwordEncripter = new PasswordEncrypterBuilder();
            var writeRepository = UserWriteOnlyRepositoryBuilder.Build();
            var unitOfWork = UnitOfWorkBuilder.Build();
            var tokenGenerator = JwtTokenGeneratorBuilder.Build();
            var readRepository = new UserReadOnlyRepositoryBuilder().Build();

            if ( string.IsNullOrWhiteSpace(email) == false) {
                readRepository.ExistActiveUserWithEmail(email);
            }

            return new RegisterUserUseCase(mapper, passwordEncripter.Build(), readRepository, writeRepository , unitOfWork , tokenGenerator);

        }

    }
}
