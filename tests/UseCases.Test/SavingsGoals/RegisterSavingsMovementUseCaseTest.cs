using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using FluentAssertions;
using MiCash.Application.UseCases.SavingsGoals.Movements;
using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Moq;

namespace UseCases.Test.SavingsGoals
{
    public class RegisterSavingsMovementUseCaseTest
    {
        private static readonly DateOnly Today = new(2026 , 10 , 15);

        [Fact]
        public async Task Concurrent_Withdrawal_Is_Retried_With_Fresh_Balance_And_Rejected ()
        {
            // Balance 300 when first read; a concurrent withdrawal of 200 commits in between,
            // so our commit conflicts and the retry sees only 100 left.
            var scenario = new Scenario(balances: [300 , 100] , conflictsBeforeSuccess: 1);

            var act = async () => await scenario.UseCase.Execute(1 , SavingsMovementType.Withdrawal , Request(200) , CancellationToken.None);

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle()
                .Which.Should().Be(ResourceErrorMessages.SAVINGS_WITHDRAWAL_EXCEEDS_BALANCE);

            scenario.Commits.Should().Be(1);
        }

        [Fact]
        public async Task Concurrent_Withdrawal_Is_Retried_And_Succeeds_When_Balance_Allows ()
        {
            var scenario = new Scenario(balances: [300 , 250] , conflictsBeforeSuccess: 1);

            await scenario.UseCase.Execute(1 , SavingsMovementType.Withdrawal , Request(200) , CancellationToken.None);

            scenario.Commits.Should().Be(2);
            scenario.AddedMovements.Should().ContainSingle();
        }

        [Fact]
        public async Task Persistent_Conflict_Surfaces_As_409_After_Max_Attempts ()
        {
            var scenario = new Scenario(balances: [1000] , conflictsBeforeSuccess: int.MaxValue);

            var act = async () => await scenario.UseCase.Execute(1 , SavingsMovementType.Deposit , Request(50) , CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();

            scenario.Commits.Should().Be(RegisterSavingsMovementUseCase.MAX_ATTEMPTS);
        }

        [Fact]
        public async Task Movement_Touches_The_Goal_So_The_Version_Is_Checked ()
        {
            var scenario = new Scenario(balances: [1000] , conflictsBeforeSuccess: 0);
            var before = scenario.Goal.UpdatedAt = DateTime.UtcNow.AddDays(-1);

            await scenario.UseCase.Execute(1 , SavingsMovementType.Deposit , Request(50) , CancellationToken.None);

            scenario.Goal.UpdatedAt.Should().BeAfter(before);
        }

        private static RequestSavingsMovementJson Request ( decimal amount ) => new() { Amount = amount , Date = Today };

        private sealed class Scenario
        {
            public RegisterSavingsMovementUseCase UseCase { get; }
            public SavingsGoal Goal { get; }
            public int Commits { get; private set; }
            public List<SavingsGoalMovement> AddedMovements { get; } = [];

            public Scenario ( decimal[] balances , int conflictsBeforeSuccess )
            {
                var user = UserBuilder.Build();
                Goal = new SavingsGoal { Id = 1 , UserId = user.Id , Name = "Viagem" , TargetAmount = 5000 };

                var updateRepository = new Mock<ISavingsGoalUpdateOnlyRepository>();
                updateRepository
                    .Setup(r => r.GetByIdAsync(user.Id , 1 , It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => Goal);

                var balanceReads = 0;
                var readRepository = new Mock<ISavingsGoalReadOnlyRepository>();
                readRepository
                    .Setup(r => r.GetSavedAmountAsync(1 , It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => balances[Math.Min(balanceReads++ , balances.Length - 1)]);

                var writeRepository = new Mock<ISavingsGoalWriteOnlyRepository>();
                writeRepository
                    .Setup(r => r.AddMovementAsync(It.IsAny<SavingsGoalMovement>() , It.IsAny<CancellationToken>()))
                    .Callback<SavingsGoalMovement , CancellationToken>(( m , _ ) => AddedMovements.Add(m))
                    .Returns(Task.CompletedTask);

                var unitOfWork = new Mock<IUnitOfWork>();
                unitOfWork
                    .Setup(u => u.Commit(It.IsAny<CancellationToken>()))
                    .Returns(() => {
                        Commits++;

                        if ( Commits <= conflictsBeforeSuccess ) {
                            // Like the real UnitOfWork: nothing persisted, change tracker cleared.
                            AddedMovements.Clear();
                            throw new ConflictException();
                        }

                        return Task.CompletedTask;
                    });

                var clock = new Mock<IBrazilClock>();
                clock.Setup(c => c.Today).Returns(Today);

                UseCase = new RegisterSavingsMovementUseCase(
                    updateRepository.Object ,
                    readRepository.Object ,
                    writeRepository.Object ,
                    LoggedUserBuilder.Build(user) ,
                    unitOfWork.Object ,
                    clock.Object);
            }
        }
    }
}
