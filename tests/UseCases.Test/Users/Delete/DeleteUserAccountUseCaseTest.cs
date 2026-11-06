using CommonTestUtilities.Cryptography;
using CommonTestUtilities.Entities;
using CommonTestUtilities.Jobs;
using CommonTestUtilities.LoggedUser;
using FluentAssertions;
using MiCash.Application.UseCases.Users.Delete;
using MiCash.Communication.Requests.Users;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Services.Payments;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UseCases.Test.Users.Delete
{
    public class DeleteUserAccountUseCaseTest
    {
        private const string PASSWORD = "Senha@123";

        private readonly User _user = UserBuilder.Build();
        private readonly Mock<IUserWriteOnlyRepository> _users = new();
        private readonly Mock<ISubscriptionsReadOnlyRepository> _subscriptions = new();
        private readonly Mock<IPaymentGateway> _gateway = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly FakeBackgroundJobService _jobs = new();

        [Fact]
        public async Task Cancels_The_Subscription_At_PagarMe_Before_Deleting ()
        {
            Billing(new Subscription { Id = 7 , Status = SubscriptionStatus.Active , PagarmeSubscriptionId = "sub_1" });

            await UseCase().Execute(new RequestDeleteAccountJson { Password = PASSWORD } , CancellationToken.None);

            _gateway.Verify(g => g.CancelSubscriptionAsync("sub_1" , It.IsAny<CancellationToken>()) , Times.Once);
            _users.Verify(u => u.Delete(_user) , Times.Once);
            _unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Once);
            _jobs.EnqueuedAccountDeletedEmails.Should().Equal((_user.Id, _user.Email, _user.Name));
        }

        [Fact]
        public async Task Wrong_Password_Deletes_Nothing ()
        {
            var act = () => UseCase().Execute(new RequestDeleteAccountJson { Password = "errada" } , CancellationToken.None);

            await act.Should().ThrowAsync<ErrorOnValidationException>();
            _users.Verify(u => u.Delete(It.IsAny<User>()) , Times.Never);
            _gateway.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PagarMe_Failure_Keeps_The_Account ()
        {
            Billing(new Subscription { Id = 7 , Status = SubscriptionStatus.Active , PagarmeSubscriptionId = "sub_1" });
            _gateway.Setup(g => g.CancelSubscriptionAsync("sub_1" , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Pagar.me offline"));

            var act = () => UseCase().Execute(new RequestDeleteAccountJson { Password = PASSWORD } , CancellationToken.None);

            await act.Should().ThrowAsync<ServiceUnavailableException>();
            _users.Verify(u => u.Delete(It.IsAny<User>()) , Times.Never);
            _unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Never);
            _jobs.EnqueuedAccountDeletedEmails.Should().BeEmpty();
        }

        private List<Subscription> _history = [];

        private void Billing ( params Subscription[] subscriptions ) => _history = subscriptions.ToList();

        private DeleteUserAccountUseCase UseCase ()
        {
            _subscriptions.Setup(s => s.GetHistoryByUserIdAsync(_user.Id , It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _history);

            return new DeleteUserAccountUseCase(
                LoggedUserBuilder.Build(_user) ,
                _users.Object ,
                _subscriptions.Object ,
                _gateway.Object ,
                new PasswordEncrypterBuilder().Verify(PASSWORD).Build() ,
                _jobs ,
                _unitOfWork.Object ,
                NullLogger<DeleteUserAccountUseCase>.Instance);
        }
    }
}
