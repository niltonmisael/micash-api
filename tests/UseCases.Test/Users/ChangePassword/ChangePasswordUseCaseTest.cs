using CommonTestUtilities.Cache;
using CommonTestUtilities.Cryptography;
using CommonTestUtilities.Time;
using MiCash.Domain.Services.Cache.Models;
using Microsoft.Extensions.Logging.Abstractions;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.Users.ChangePassword;
using MiCash.Domain.Entities;
using MiCash.Exception.ExceptionBase;
using MiCash.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.Test.Users.ChangePassword
{
    public class ChangePasswordUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var cancellationToken = CancellationToken.None;
            var user = UserBuilder.Build();

            var request = RequestChangePasswordJsonBuilder.Build();

            var useCase = CreateUseCase(user , request.Password);

            var act = async () => await useCase.Execute(request, cancellationToken);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Error_NewPassword_Empty ()
        {
            var cancellationToken = CancellationToken.None;
            var user = UserBuilder.Build();

            var request = RequestChangePasswordJsonBuilder.Build();
            request.NewPassword = string.Empty;

            var useCase = CreateUseCase(user , request.Password);

            var act = async () => { await useCase.Execute(request, cancellationToken); };

            var result = await act.Should().ThrowAsync<ErrorOnValidationException>();

            result.Where(e => e.GetErrors().Count == 1 &&
                    e.GetErrors().Contains(ResourceErrorMessages.INVALID_PASSWORD));
        }

        [Fact]
        public async Task Error_CurrentPassword_Different ()
        {
            var cancellationToken = CancellationToken.None;
            var user = UserBuilder.Build();
            var request = RequestChangePasswordJsonBuilder.Build();

            var useCase = CreateUseCase(user);

            var act = async () => { await useCase.Execute(request, cancellationToken); };

            var result = await act.Should().ThrowAsync<ErrorOnValidationException>();

            result.Where(e => e.GetErrors().Count == 1 &&
                    e.GetErrors().Contains(ResourceErrorMessages.PASSWORD_DIFFERENT_CURRENT_PASSWORD));
        }

        [Fact]
        public async Task Error_NewPassword_Same_As_Current ()
        {
            var user = UserBuilder.Build();
            var request = RequestChangePasswordJsonBuilder.Build();
            request.Password = "SenhaAtual1!";
            request.NewPassword = "SenhaAtual1!";
            var cache = new FakeRedisCacheService();

            var act = () => CreateUseCase(user , request.Password , cache).Execute(request , CancellationToken.None);

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.NEW_PASSWORD_SAME_AS_CURRENT);
            (await cache.GetAsync<PasswordChangedData>(PasswordChangedData.Key(user.UserIdentifier))).Should().BeNull();
        }

        [Fact]
        public async Task Sessions_Opened_Before_The_Change_Can_No_Longer_Be_Renewed ()
        {
            var user = UserBuilder.Build();
            var request = RequestChangePasswordJsonBuilder.Build();
            var cache = new FakeRedisCacheService();
            var time = new MutableTimeProvider(new DateTimeOffset(2026 , 11 , 2 , 12 , 0 , 0 , TimeSpan.Zero));

            await CreateUseCase(user , request.Password , cache , time).Execute(request , CancellationToken.None);

            var changed = await cache.GetAsync<PasswordChangedData>(PasswordChangedData.Key(user.UserIdentifier));
            changed!.ChangedAt.Should().Be(time.UtcNow);
        }

        [Fact]
        public async Task Wrong_Current_Password_Does_Not_Revoke_Sessions ()
        {
            var user = UserBuilder.Build();
            var cache = new FakeRedisCacheService();

            var act = () => CreateUseCase(user , cache: cache).Execute(RequestChangePasswordJsonBuilder.Build() , CancellationToken.None);

            await act.Should().ThrowAsync<ErrorOnValidationException>();
            (await cache.GetAsync<PasswordChangedData>(PasswordChangedData.Key(user.UserIdentifier))).Should().BeNull();
        }

        private static ChangePasswordUseCase CreateUseCase (
            User user ,
            string? password = null ,
            FakeRedisCacheService? cache = null ,
            TimeProvider? time = null )
        {
            var unitOfWork = UnitOfWorkBuilder.Build();
            var userUpdateRepository = UserUpdateOnlyRepositoryBuilder.Build(user);
            var loggedUser = LoggedUserBuilder.Build(user);
            var passwordEncripter = new PasswordEncrypterBuilder().Verify(password).Build();

            return new ChangePasswordUseCase(
                loggedUser , userUpdateRepository , unitOfWork , passwordEncripter ,
                cache ?? new FakeRedisCacheService() ,
                time ?? TimeProvider.System ,
                NullLogger<ChangePasswordUseCase>.Instance);
        }
    }
}
