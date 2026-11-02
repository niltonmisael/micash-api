using FluentAssertions;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Dashboards
{
    public class DailySummaryTest : MiCashClassFixture
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly string _token;

        public DailySummaryTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
            _token = factory.GetToken();
        }

        [Fact]
        public async Task Last_Day_Cumulative_Matches_The_Monthly_Summary ()
        {
            // A fixed past month, so the factory's seed (today) does not interfere.
            const int year = 2026;
            const int month = 2;
            await Seed(year , month);

            var daily = await ReadJson(await DoGet($"api/dashboard/daily-summary?year={year}&month={month}" , _token));
            var monthly = await ReadJson(await DoGet($"api/dashboard/monthly-summary?year={year}&month={month}" , _token));

            var days = daily.GetProperty("days").EnumerateArray().ToList();
            days.Should().HaveCount(28);
            days[0].GetProperty("day").GetInt32().Should().Be(1);

            var last = days[^1];
            last.GetProperty("cumulativeIncome").GetDecimal().Should().Be(monthly.GetProperty("totalIncome").GetDecimal()).And.Be(3500m);
            last.GetProperty("cumulativeExpense").GetDecimal().Should().Be(monthly.GetProperty("totalExpense").GetDecimal()).And.Be(330m);

            days[9].GetProperty("expense").GetDecimal().Should().Be(300m);
            days[9].GetProperty("cumulativeExpense").GetDecimal().Should().Be(300m);
            days[10].GetProperty("expense").GetDecimal().Should().Be(0m);
        }

        [Fact]
        public async Task Invalid_Month_Is_400 ()
        {
            (await DoGet("api/dashboard/daily-summary?year=2026&month=13" , _token)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Without_Token_Is_401 ()
        {
            (await DoGet("api/dashboard/daily-summary?year=2026&month=2" , token: string.Empty)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private async Task Seed ( int year , int month )
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();

            var userId = await db.Users.Where(u => u.Email == _factory.GetEmail()).Select(u => u.Id).SingleAsync();

            db.Incomes.AddRange(
                new Income { UserId = userId , Title = "Salário" , Amount = 3000m , ReceivedAt = new DateOnly(year , month , 5) , Category = IncomeCategory.Salary } ,
                new Income { UserId = userId , Title = "Extra" , Amount = 500m , ReceivedAt = new DateOnly(year , month , 28) , Category = IncomeCategory.Salary });

            db.Expenses.AddRange(
                new Expense { UserId = userId , CategoryId = _factory.GetCategoryId() , Title = "Aluguel" , Amount = 200m , ExpenseDate = new DateTime(year , month , 10 , 12 , 0 , 0 , DateTimeKind.Utc) } ,
                new Expense { UserId = userId , CategoryId = _factory.GetCategoryId() , Title = "Luz" , Amount = 100m , ExpenseDate = new DateTime(year , month , 10 , 15 , 0 , 0 , DateTimeKind.Utc) } ,
                new Expense { UserId = userId , CategoryId = _factory.GetCategoryId() , Title = "Café" , Amount = 30m , ExpenseDate = new DateTime(year , month , 28 , 9 , 0 , 0 , DateTimeKind.Utc) });

            await db.SaveChangesAsync();
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
