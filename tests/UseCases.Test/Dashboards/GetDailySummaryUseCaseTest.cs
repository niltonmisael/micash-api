using CommonTestUtilities.Cache;
using FluentAssertions;
using MiCash.Application.UseCases.Dashboards.DailySummary;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Repositories.Dashboards.DTOs;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception.ExceptionBase;
using Moq;

namespace UseCases.Test.Dashboards
{
    public class GetDailySummaryUseCaseTest
    {
        [Fact]
        public void Every_Day_Of_The_Month_Is_Returned_With_Running_Totals ()
        {
            var summary = GetDailySummaryUseCase.Build(
                2026 , 11 ,
                incomes: [new DailyAmount { Day = 5 , Amount = 6000m }] ,
                expenses: [new DailyAmount { Day = 1 , Amount = 250m } , new DailyAmount { Day = 5 , Amount = 100.50m } , new DailyAmount { Day = 30 , Amount = 49.50m }]);

            summary.Year.Should().Be(2026);
            summary.Month.Should().Be(11);
            summary.Days.Should().HaveCount(30);
            summary.Days.Select(d => d.Day).Should().Equal(Enumerable.Range(1 , 30));

            summary.Days[0].Should().BeEquivalentTo(new { Day = 1 , Income = 0m , Expense = 250m , CumulativeIncome = 0m , CumulativeExpense = 250m });
            summary.Days[1].Should().BeEquivalentTo(new { Day = 2 , Income = 0m , Expense = 0m , CumulativeIncome = 0m , CumulativeExpense = 250m });
            summary.Days[4].Should().BeEquivalentTo(new { Day = 5 , Income = 6000m , Expense = 100.50m , CumulativeIncome = 6000m , CumulativeExpense = 350.50m });
            summary.Days[^1].Should().BeEquivalentTo(new { Day = 30 , CumulativeIncome = 6000m , CumulativeExpense = 400m });
        }

        [Theory]
        [InlineData(2028 , 2 , 29)]
        [InlineData(2026 , 2 , 28)]
        [InlineData(2026 , 12 , 31)]
        public void Month_Length_Follows_The_Calendar ( int year , int month , int days )
        {
            GetDailySummaryUseCase.Build(year , month , [] , []).Days.Should().HaveCount(days)
                .And.OnlyContain(d => d.Income == 0 && d.Expense == 0 && d.CumulativeIncome == 0 && d.CumulativeExpense == 0);
        }

        [Theory]
        [InlineData(0 , 2026)]
        [InlineData(13 , 2026)]
        [InlineData(5 , 1999)]
        public async Task Invalid_Period_Is_Refused ( int month , int year )
        {
            var act = () => UseCase(new Mock<IDashboardReadOnlyRepository>()).Execute(year , month , CancellationToken.None);

            await act.Should().ThrowAsync<ErrorOnValidationException>();
        }

        [Fact]
        public async Task Result_Is_Cached ()
        {
            var repository = new Mock<IDashboardReadOnlyRepository>();
            repository.Setup(r => r.GetDailyIncomesByMonthAsync(1 , 2026 , 11 , It.IsAny<CancellationToken>())).ReturnsAsync([]);
            repository.Setup(r => r.GetDailyExpensesByMonthAsync(1 , 2026 , 11 , It.IsAny<CancellationToken>()))
                .ReturnsAsync([new DailyAmount { Day = 2 , Amount = 10m }]);
            var cache = new FakeRedisCacheService();

            var first = await UseCase(repository , cache).Execute(2026 , 11 , CancellationToken.None);
            var second = await UseCase(repository , cache).Execute(2026 , 11 , CancellationToken.None);

            second.Should().BeSameAs(first);
            repository.Verify(r => r.GetDailyExpensesByMonthAsync(1 , 2026 , 11 , It.IsAny<CancellationToken>()) , Times.Once);
        }

        private static GetDailySummaryUseCase UseCase ( Mock<IDashboardReadOnlyRepository> repository , FakeRedisCacheService? cache = null )
        {
            var loggedUser = new Mock<ILoggedUser>();
            loggedUser.Setup(l => l.Get()).ReturnsAsync(new User { Id = 1 });

            return new GetDailySummaryUseCase(loggedUser.Object , repository.Object , cache ?? new FakeRedisCacheService());
        }
    }
}
