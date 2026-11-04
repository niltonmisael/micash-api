using CommonTestUtilities.Cache;
using FluentAssertions;
using MiCash.Application.UseCases.Dashboards.DailySummary;
using MiCash.Application.UseCases.Dashboards.MonthForecast;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Repositories.Dashboards.DTOs;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Forecast;
using MiCash.Domain.Services.LoggedUser;
using Moq;

namespace UseCases.Test.Dashboards
{
    public class MonthForecastCalculatorTest
    {
        private static ForecastExpense E ( int day , decimal amount , long categoryId = 1 ) => new(day , categoryId , amount);

        [Fact]
        public void Spending_Pace_Continues_Until_The_End_Of_The_Month ()
        {
            // November: 30 days. 10 days elapsed, 1000 spent => 100/day, 20 days left.
            var forecast = MonthForecastCalculator.Calculate(
                new DateOnly(2026 , 11 , 10) , income: 5000m ,
                [E(1 , 400m) , E(4 , 350m) , E(10 , 250m)] , []);

            forecast.Should().BeEquivalentTo(new
            {
                Year = 2026 , Month = 11 , Today = 10 , DaysInMonth = 30 , IsReliable = true ,
                Income = 5000m , ExpenseToDate = 1000m , ScheduledExpense = 0m ,
                DailyExpenseRate = 100m , ProjectedExpense = 3000m , ProjectedBalance = 2000m
            });
        }

        [Fact]
        public void Expenses_Registered_After_Today_Are_Added_Not_Extrapolated ()
        {
            var forecast = MonthForecastCalculator.Calculate(
                new DateOnly(2026 , 11 , 10) , 0m ,
                [E(5 , 1000m) , E(20 , 1500m)] , []);

            forecast.ExpenseToDate.Should().Be(1000m);
            forecast.ScheduledExpense.Should().Be(1500m);
            forecast.DailyExpenseRate.Should().Be(100m);
            forecast.ProjectedExpense.Should().Be(1000m + 100m * 20 + 1500m);
            forecast.ProjectedBalance.Should().Be(-4500m);
        }

        [Fact]
        public void Goal_Exceeded_On_The_Expected_Day ()
        {
            // 300 in 10 days => 30/day: 300 + 30*(d-10) > 500  =>  d = 17 (510).
            var forecast = MonthForecastCalculator.Calculate(
                new DateOnly(2026 , 11 , 10) , 0m ,
                [E(2 , 100m) , E(9 , 200m) , E(3 , 999m , categoryId: 2)] ,
                [new ForecastGoal(1 , "Alimentação" , 500m)]);

            forecast.Goals.Single().Should().BeEquivalentTo(new
            {
                CategoryId = 1L , CategoryName = "Alimentação" , TargetAmount = 500m , CurrentAmount = 300m ,
                ProjectedAmount = 900m , AlreadyExceeded = false , WillExceed = true , EstimatedExceedDay = 17
            });
        }

        [Fact]
        public void Scheduled_Expense_Brings_The_Exceed_Day_Forward ()
        {
            // 100 in 10 days => 10/day; on day 12 a 400 bill is already registered: 100 + 20 + 400 > 500.
            var goal = MonthForecastCalculator.Calculate(
                new DateOnly(2026 , 11 , 10) , 0m ,
                [E(1 , 100m) , E(12 , 400m)] ,
                [new ForecastGoal(1 , "Casa" , 500m)]).Goals.Single();

            goal.CurrentAmount.Should().Be(500m);
            goal.AlreadyExceeded.Should().BeTrue("500 of 500 is 100%, the same threshold as the goal progress");
            goal.WillExceed.Should().BeFalse();
            goal.EstimatedExceedDay.Should().BeNull();
        }

        [Fact]
        public void Goal_Within_Budget ()
        {
            var goal = MonthForecastCalculator.Calculate(
                new DateOnly(2026 , 11 , 15) , 0m , [E(1 , 150m)] ,
                [new ForecastGoal(1 , "Lazer" , 500m) , new ForecastGoal(7 , "Sem gasto" , 100m)]).Goals;

            goal[0].Should().BeEquivalentTo(new { ProjectedAmount = 300m , WillExceed = false , AlreadyExceeded = false , EstimatedExceedDay = (int?) null });
            goal[1].Should().BeEquivalentTo(new { CurrentAmount = 0m , ProjectedAmount = 0m , WillExceed = false });
        }

        [Fact]
        public void Goal_Already_Exceeded ()
        {
            var goal = MonthForecastCalculator.Calculate(
                new DateOnly(2026 , 11 , 10) , 0m , [E(3 , 700m)] ,
                [new ForecastGoal(1 , "Lazer" , 500m)]).Goals.Single();

            goal.AlreadyExceeded.Should().BeTrue();
            goal.WillExceed.Should().BeFalse();
            goal.EstimatedExceedDay.Should().BeNull();
        }

        [Fact]
        public void Month_Without_Expenses ()
        {
            var forecast = MonthForecastCalculator.Calculate(new DateOnly(2026 , 11 , 20) , 3000m , [] , []);

            forecast.ProjectedExpense.Should().Be(0m);
            forecast.ProjectedBalance.Should().Be(3000m);
            forecast.IsReliable.Should().BeFalse();
        }

        [Theory]
        [InlineData(4 , 3 , false)]
        [InlineData(5 , 3 , true)]
        [InlineData(5 , 2 , false)]
        public void Reliability_Needs_Days_And_Expenses ( int day , int expenses , bool reliable )
        {
            var list = Enumerable.Range(1 , expenses).Select(_ => E(1 , 10m)).ToList();

            MonthForecastCalculator.Calculate(new DateOnly(2026 , 11 , day) , 0m , list , []).IsReliable.Should().Be(reliable);
        }

        [Fact]
        public void On_The_Last_Day_The_Forecast_Is_The_Daily_Summary_Total ()
        {
            var expenses = new[] { E(1 , 10.33m) , E(15 , 200m) , E(29 , 0.01m) };

            var forecast = MonthForecastCalculator.Calculate(new DateOnly(2028 , 2 , 29) , 0m , expenses , []);
            var daily = GetDailySummaryUseCase.Build(2028 , 2 , [] ,
                expenses.Select(e => new DailyAmount { Day = e.Day , Amount = e.Amount }));

            forecast.DaysInMonth.Should().Be(29);
            forecast.ProjectedExpense.Should().Be(daily.Days[^1].CumulativeExpense);
        }
    }

    public class GetMonthForecastUseCaseTest
    {
        private const long USER_ID = 1;

        [Fact]
        public async Task Forecast_Of_The_Current_Brasilia_Month_With_Goals ()
        {
            var (dashboard, expenses, goals) = Repositories(2026 , 11);

            var response = await UseCase(dashboard , expenses , goals , new FakeRedisCacheService() , new DateOnly(2026 , 11 , 10))
                .Execute(CancellationToken.None);

            response.Should().BeEquivalentTo(new { Year = 2026 , Month = 11 , Today = 10 , Income = 4000m , ExpenseToDate = 300m , ProjectedExpense = 900m , ProjectedBalance = 3100m });
            response.Goals.Single().Should().BeEquivalentTo(new { CategoryName = "Mercado" , WillExceed = true , EstimatedExceedDay = 17 });
        }

        [Fact]
        public async Task Cached_Forecast_Is_Reused_The_Same_Day_Only ()
        {
            var (dashboard, expenses, goals) = Repositories(2026 , 11);
            var cache = new FakeRedisCacheService();

            var first = await UseCase(dashboard , expenses , goals , cache , new DateOnly(2026 , 11 , 10)).Execute(CancellationToken.None);
            var sameDay = await UseCase(dashboard , expenses , goals , cache , new DateOnly(2026 , 11 , 10)).Execute(CancellationToken.None);
            var nextDay = await UseCase(dashboard , expenses , goals , cache , new DateOnly(2026 , 11 , 11)).Execute(CancellationToken.None);

            sameDay.Should().BeSameAs(first);
            nextDay.Today.Should().Be(11);
            expenses.Verify(r => r.GetByPeriodAsync(USER_ID , 11 , 2026 , It.IsAny<CancellationToken>()) , Times.Exactly(2));
        }

        private static (Mock<IDashboardReadOnlyRepository>, Mock<IExpensesReadOnlyRepository>, Mock<IFinancialGoalReadOnlyRepository>) Repositories ( int year , int month )
        {
            var dashboard = new Mock<IDashboardReadOnlyRepository>();
            dashboard.Setup(r => r.GetDailyIncomesByMonthAsync(USER_ID , year , month , It.IsAny<CancellationToken>()))
                .ReturnsAsync([new DailyAmount { Day = 5 , Amount = 4000m }]);

            var expenses = new Mock<IExpensesReadOnlyRepository>();
            expenses.Setup(r => r.GetByPeriodAsync(USER_ID , month , year , It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new Expense { CategoryId = 1 , Amount = 100m , ExpenseDate = new DateTime(year , month , 2 , 12 , 0 , 0 , DateTimeKind.Utc) } ,
                    new Expense { CategoryId = 1 , Amount = 200m , ExpenseDate = new DateTime(year , month , 9 , 12 , 0 , 0 , DateTimeKind.Utc) }
                ]);

            var goals = new Mock<IFinancialGoalReadOnlyRepository>();
            goals.Setup(r => r.GetByPeriodAsync(USER_ID , month , year , It.IsAny<CancellationToken>()))
                .ReturnsAsync([new FinancialGoal { CategoryId = 1 , TargetAmount = 500m , Category = new Category { Id = 1 , Name = "Mercado" } }]);

            return (dashboard, expenses, goals);
        }

        private static GetMonthForecastUseCase UseCase (
            Mock<IDashboardReadOnlyRepository> dashboard ,
            Mock<IExpensesReadOnlyRepository> expenses ,
            Mock<IFinancialGoalReadOnlyRepository> goals ,
            FakeRedisCacheService cache ,
            DateOnly today )
        {
            var loggedUser = new Mock<ILoggedUser>();
            loggedUser.Setup(l => l.Get()).ReturnsAsync(new User { Id = USER_ID });

            var clock = new Mock<IBrazilClock>();
            clock.Setup(c => c.Today).Returns(today);

            return new GetMonthForecastUseCase(loggedUser.Object , dashboard.Object , expenses.Object , goals.Object , cache , clock.Object);
        }
    }
}
