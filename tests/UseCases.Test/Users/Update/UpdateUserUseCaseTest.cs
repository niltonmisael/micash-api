using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.Users.Update;
using MiCash.Domain.Entities;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace UseCases.Test.Users.Update
{
    public class UpdateUserUseCaseTest
    {
        [Fact]
        public async Task Success_Updates_The_Name_With_The_Current_Email ()
        {
            var cancellationToken = CancellationToken.None;
            var user = UserBuilder.Build();
            var currentEmail = user.Email;
            var request = RequestUpdateUserJsonBuilder.Build();
            request.Email = currentEmail;

            var useCase = CreateUseCase(user);

            var act = async () => await useCase.Execute(request, cancellationToken);

            await act.Should().NotThrowAsync();

            user.Name.Should().Be(request.Name);
            user.Email.Should().Be(currentEmail);
        }

        [Fact]
        public async Task Error_Name_Empty ()
        {
            var cancellationToken = CancellationToken.None;
            var user = UserBuilder.Build();

            var request = RequestUpdateUserJsonBuilder.Build();
            request.Email = user.Email;
            request.Name = string.Empty;

            var useCase = CreateUseCase(user);

            var act = async () => await useCase.Execute(request, cancellationToken);

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle(ResourceErrorMessages.NAME_EMPTY);
        }

        [Fact]
        public async Task Different_Email_Requires_The_Confirmation_Flow_And_Changes_Nothing ()
        {
            var user = UserBuilder.Build();
            var currentEmail = user.Email;
            var currentName = user.Name;
            var request = RequestUpdateUserJsonBuilder.Build();
            request.Email = "outro." + currentEmail;

            var act = () => CreateUseCase(user).Execute(request , CancellationToken.None);

            var error = (await act.Should().ThrowAsync<EmailChangeRequiresConfirmationException>()).Which;
            error.ErrorCode.Should().Be("EMAIL_CHANGE_REQUIRES_CONFIRMATION");
            error.StatusCode.Should().Be(400);
            user.Email.Should().Be(currentEmail);
            user.Name.Should().Be(currentName);
        }

        private static UpdateUserUseCase CreateUseCase ( User user )
        {
            var unitOfWork = UnitOfWorkBuilder.Build();
            var updateRepository = UserUpdateOnlyRepositoryBuilder.Build(user);
            var loggedUser = LoggedUserBuilder.Build(user);

            return new UpdateUserUseCase(loggedUser , unitOfWork, updateRepository);
        }
    }
}
