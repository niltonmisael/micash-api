using CommonTestUtilities.Plans;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Time;
using FluentAssertions;
using MiCash.Application.Services.FinancialGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.FinancialGoals;
using MiCash.Domain.Services.Plans;
using MiCash.Domain.Services.PushNotifications;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UseCases.Test.FinancialGoals
{
    public class FinancialGoalAlertNotifierTest
    {
        private static readonly User USER = new() { Id = 7 };
        private static readonly Category FOOD = new() { Id = 3 , Name = "Alimentação" };

        // 2026-11-10 15:00 UTC = 12:00 in Brasília.
        private static readonly DateTimeOffset NOW = new(2026 , 11 , 10 , 15 , 0 , 0 , TimeSpan.Zero);
        private static readonly DateTime EXPENSE_DATE = NOW.UtcDateTime;

        [Theory]
        [InlineData(79.99 , null)]
        [InlineData(80 , FinancialGoalAlertNotifier.TYPE_WARNING)]
        [InlineData(99.99 , FinancialGoalAlertNotifier.TYPE_WARNING)]
        [InlineData(100 , FinancialGoalAlertNotifier.TYPE_EXCEEDED)]
        [InlineData(250 , FinancialGoalAlertNotifier.TYPE_EXCEEDED)]
        public async Task Premium_Gets_The_80_And_100_Percent_Alerts ( decimal spent , string? expectedType )
        {
            var push = new Mock<IPushNotificationService>();

            await Notifier(push , PlanLimits.Premium , spent).NotifyAsync(USER , FOOD.Id , EXPENSE_DATE , CancellationToken.None);

            if ( expectedType is null ) {
                push.VerifyNoOtherCalls();
            } else {
                push.Verify(p => p.EnqueueAsync(USER.Id , It.Is<PushMessage>(m => m.Type == expectedType) , It.IsAny<CancellationToken>()) , Times.Once);
            }
        }

        [Theory]
        [InlineData(90 , false)]
        [InlineData(100 , true)]
        public async Task Free_Gets_Only_The_Exceeded_Alert ( decimal spent , bool alerted )
        {
            var push = new Mock<IPushNotificationService>();

            await Notifier(push , PlanLimits.Free , spent).NotifyAsync(USER , FOOD.Id , EXPENSE_DATE , CancellationToken.None);

            push.Verify(
                p => p.EnqueueAsync(USER.Id , It.Is<PushMessage>(m => m.Type == FinancialGoalAlertNotifier.TYPE_EXCEEDED) , It.IsAny<CancellationToken>()) ,
                alerted ? Times.Once() : Times.Never());
            push.Verify(
                p => p.EnqueueAsync(It.IsAny<long>() , It.Is<PushMessage>(m => m.Type == FinancialGoalAlertNotifier.TYPE_WARNING) , It.IsAny<CancellationToken>()) ,
                Times.Never);
        }

        [Fact]
        public async Task Message_Opens_The_Goals_And_Is_Sent_Once_Per_Goal_And_Target ()
        {
            PushMessage? sent = null;
            var push = new Mock<IPushNotificationService>();
            push.Setup(p => p.EnqueueAsync(USER.Id , It.IsAny<PushMessage>() , It.IsAny<CancellationToken>()))
                .Callback<long , PushMessage , CancellationToken>(( _ , message , _ ) => sent = message)
                .ReturnsAsync(new PushEnqueueResult(1 , 1));

            await Notifier(push , PlanLimits.Premium , spent: 125m).NotifyAsync(USER , FOOD.Id , EXPENSE_DATE , CancellationToken.None);

            sent.Should().NotBeNull();
            sent!.Title.Should().Be("Meta de Alimentação estourada");
            sent.Body.Should().Contain("R$ 25,00").And.Contain("R$ 125,00 de R$ 100,00");
            sent.Route.Should().Be(FinancialGoalAlertNotifier.ROUTE);
            PushRoutes.IsAllowed(sent.Route).Should().BeTrue();
            sent.IdempotencyKey.Should().Be("goal-exceeded:11:10000");
        }

        [Fact]
        public async Task Warning_Message_Shows_The_Used_Percentage ()
        {
            PushMessage? sent = null;
            var push = new Mock<IPushNotificationService>();
            push.Setup(p => p.EnqueueAsync(USER.Id , It.IsAny<PushMessage>() , It.IsAny<CancellationToken>()))
                .Callback<long , PushMessage , CancellationToken>(( _ , message , _ ) => sent = message)
                .ReturnsAsync(new PushEnqueueResult(1 , 1));

            await Notifier(push , PlanLimits.Premium , spent: 85.5m).NotifyAsync(USER , FOOD.Id , EXPENSE_DATE , CancellationToken.None);

            sent!.Title.Should().Be("Meta de Alimentação quase no limite");
            sent.Body.Should().StartWith("Você já usou 85% da meta de Alimentação");
            sent.IdempotencyKey.Should().Be("goal-warning:11:10000");
        }

        [Fact]
        public async Task Past_Month_Expense_Does_Not_Alert ()
        {
            var push = new Mock<IPushNotificationService>();

            // 01:30 UTC on Nov 1st = 22:30 on Oct 31st in Brasília: October, already closed.
            await Notifier(push , PlanLimits.Premium , spent: 500m)
                .NotifyAsync(USER , FOOD.Id , new DateTime(2026 , 11 , 1 , 1 , 30 , 0 , DateTimeKind.Utc) , CancellationToken.None);

            push.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Category_Without_Goal_Does_Not_Alert ()
        {
            var push = new Mock<IPushNotificationService>();

            await Notifier(push , PlanLimits.Premium , spent: 500m).NotifyAsync(USER , categoryId: 999 , EXPENSE_DATE , CancellationToken.None);

            push.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task A_Push_Failure_Never_Reaches_The_Expense ()
        {
            var push = new Mock<IPushNotificationService>();
            push.Setup(p => p.EnqueueAsync(It.IsAny<long>() , It.IsAny<PushMessage>() , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("database down"));

            var act = () => Notifier(push , PlanLimits.Premium , spent: 500m).NotifyAsync(USER , FOOD.Id , EXPENSE_DATE , CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        /// <summary>Goal of R$ 100,00 for FOOD in November 2026; `spent` split in two expenses of the category plus one of another.</summary>
        private static FinancialGoalAlertNotifier Notifier ( Mock<IPushNotificationService> push , PlanLimits plan , decimal spent )
        {
            var goal = new FinancialGoal { Id = 11 , UserId = USER.Id , CategoryId = FOOD.Id , Category = FOOD , TargetAmount = 100m , Month = 11 , Year = 2026 };

            var goals = new FinancialGoalReadOnlyRepositoryBuilder()
                .GetByPeriod(USER , 11 , 2026 , [goal])
                .Build();

            var expenses = new Mock<IExpensesReadOnlyRepository>();
            expenses.Setup(r => r.GetByPeriodAsync(USER.Id , 11 , 2026 , It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new Expense { UserId = USER.Id , CategoryId = FOOD.Id , Amount = spent / 2 } ,
                    new Expense { UserId = USER.Id , CategoryId = FOOD.Id , Amount = spent - spent / 2 } ,
                    new Expense { UserId = USER.Id , CategoryId = 999 , Amount = 1000m }
                ]);

            return new FinancialGoalAlertNotifier(
                goals ,
                expenses.Object ,
                new FinancialGoalCalculator() ,
                UserPlanServiceBuilder.Build(plan) ,
                push.Object ,
                new BrazilClock(new MutableTimeProvider(NOW)) ,
                NullLogger<FinancialGoalAlertNotifier>.Instance);
        }
    }
}
