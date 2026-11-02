using CommonTestUtilities.Cache;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using FluentAssertions;
using MiCash.Application.UseCases.FinancialGoals.Alert;
using MiCash.Application.UseCases.FinancialGoals.Progress;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Services.FinancialGoals;
using MiCash.Exception.ExceptionBase;

namespace UseCases.Test.FinancialGoals
{
    public class FinancialGoalCalculatorTest
    {
        [Theory]
        [InlineData(0 , FinancialGoalProgressStatus.Normal)]
        [InlineData(79.99 , FinancialGoalProgressStatus.Normal)]
        [InlineData(80 , FinancialGoalProgressStatus.Warning)]
        [InlineData(99.99 , FinancialGoalProgressStatus.Warning)]
        [InlineData(100 , FinancialGoalProgressStatus.Exceeded)]
        [InlineData(150 , FinancialGoalProgressStatus.Exceeded)]
        public void Status_By_Percentage ( decimal spent , FinancialGoalProgressStatus expected )
        {
            var goal = new FinancialGoal { TargetAmount = 100 };

            var result = new FinancialGoalCalculator().Calculate(goal , spent);

            result.Status.Should().Be(expected);
            result.PercentageUsed.Should().Be(Math.Round(spent , 2));
            result.RemainingAmount.Should().Be(100 - spent);
        }
    }

    public class ProgressAndAlertsUseCaseTest
    {
        private const int MONTH = 6;
        private static readonly int YEAR = DateTime.UtcNow.Year;

        [Fact]
        public async Task Progress_Sums_Expenses_By_Category ()
        {
            var user = UserBuilder.Build();
            var (goals, expenses) = BuildScenario(user);

            var useCase = new GetFinancialGoalsProgressUseCase(
                new FinancialGoalReadOnlyRepositoryBuilder().GetByPeriod(user , MONTH , YEAR , goals).Build() ,
                new ExpenseReadOnlyRepositoryBuilder().GetByPeriod(user , MONTH , YEAR , expenses).Build() ,
                LoggedUserBuilder.Build(user) ,
                new FinancialGoalCalculator() ,
                new FakeRedisCacheService());

            var response = await useCase.Execute(MONTH , YEAR , CancellationToken.None);

            response.Goals.Should().HaveCount(3);

            var normal = response.Goals.Single(g => g.CategoryId == 1);
            normal.CurrentAmount.Should().Be(50);
            normal.PercentageUsed.Should().Be(50);
            normal.Status.Should().Be(( int ) FinancialGoalProgressStatus.Normal);

            response.Goals.Single(g => g.CategoryId == 2).Status.Should().Be(( int ) FinancialGoalProgressStatus.Warning);
            response.Goals.Single(g => g.CategoryId == 3).Status.Should().Be(( int ) FinancialGoalProgressStatus.Exceeded);
        }

        [Fact]
        public async Task Alerts_Return_Only_Warning_And_Exceeded ()
        {
            var user = UserBuilder.Build();
            var (goals, expenses) = BuildScenario(user);

            var useCase = new GetFinancialGoalsAlertsUseCase(
                new FinancialGoalReadOnlyRepositoryBuilder().GetByPeriod(user , MONTH , YEAR , goals).Build() ,
                new ExpenseReadOnlyRepositoryBuilder().GetByPeriod(user , MONTH , YEAR , expenses).Build() ,
                LoggedUserBuilder.Build(user) ,
                new FinancialGoalCalculator() ,
                new FakeRedisCacheService());

            var response = await useCase.Execute(MONTH , YEAR , CancellationToken.None);

            response.Alerts.Select(a => a.CategoryId).Should().BeEquivalentTo([2L , 3L]);
            response.Alerts.Should().OnlyContain(a => !string.IsNullOrWhiteSpace(a.Message));
        }

        [Fact]
        public async Task Progress_Error_Invalid_Month ()
        {
            var user = UserBuilder.Build();

            var useCase = new GetFinancialGoalsProgressUseCase(
                new FinancialGoalReadOnlyRepositoryBuilder().Build() ,
                new ExpenseReadOnlyRepositoryBuilder().Build() ,
                LoggedUserBuilder.Build(user) ,
                new FinancialGoalCalculator() ,
                new FakeRedisCacheService());

            var act = async () => await useCase.Execute(13 , YEAR , CancellationToken.None);

            await act.Should().ThrowAsync<ErrorOnValidationException>();
        }

        // Category 1: 50/100 (Normal), category 2: 85/100 (Warning), category 3: 120/100 (Exceeded).
        // Category 4 has expenses but no goal and must be ignored.
        private static (List<FinancialGoal> goals, List<Expense> expenses) BuildScenario ( User user )
        {
            var goals = FinancialGoalBuilder.Collection(user , count: 3 , MONTH , YEAR);
            goals.ForEach(g => g.TargetAmount = 100);

            var expenses = new List<Expense>
            {
                new() { UserId = user.Id , CategoryId = 1 , Amount = 20 },
                new() { UserId = user.Id , CategoryId = 1 , Amount = 30 },
                new() { UserId = user.Id , CategoryId = 2 , Amount = 85 },
                new() { UserId = user.Id , CategoryId = 3 , Amount = 120 },
                new() { UserId = user.Id , CategoryId = 4 , Amount = 999 },
            };

            return (goals, expenses);
        }
    }
}
