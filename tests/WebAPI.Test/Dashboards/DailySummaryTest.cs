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
        public async Task Expenses_Belong_To_The_Brasilia_Month_And_Day ()
        {
            // Fixed past months (April 2026), away from the other fixtures. Brasília = UTC-3.
            await using ( var scope = _factory.Services.CreateAsyncScope() ) {
                var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();
                var userId = await db.Users.Where(u => u.Email == _factory.GetEmail()).Select(u => u.Id).SingleAsync();
                var categoryId = _factory.GetCategoryId();

                db.Expenses.AddRange(
                    // 23:59 on March 31st in Brasília: March, not April.
                    new Expense { UserId = userId , CategoryId = categoryId , Title = "Março" , Amount = 9m , ExpenseDate = new DateTime(2026 , 4 , 1 , 2 , 59 , 0 , DateTimeKind.Utc) } ,
                    // 00:00 on April 1st in Brasília.
                    new Expense { UserId = userId , CategoryId = categoryId , Title = "Início" , Amount = 5m , ExpenseDate = new DateTime(2026 , 4 , 1 , 3 , 0 , 0 , DateTimeKind.Utc) } ,
                    // 22:30 on April 30th in Brasília (already May 1st in UTC).
                    new Expense { UserId = userId , CategoryId = categoryId , Title = "Fim" , Amount = 70m , ExpenseDate = new DateTime(2026 , 5 , 1 , 1 , 30 , 0 , DateTimeKind.Utc) });

                await db.SaveChangesAsync();
            }

            var april = await ReadJson(await DoGet("api/dashboard/monthly-summary?year=2026&month=4" , _token));
            var may = await ReadJson(await DoGet("api/dashboard/monthly-summary?year=2026&month=5" , _token));
            var march = await ReadJson(await DoGet("api/dashboard/monthly-summary?year=2026&month=3" , _token));

            april.GetProperty("totalExpense").GetDecimal().Should().Be(75m);
            may.GetProperty("totalExpense").GetDecimal().Should().Be(0m);
            march.GetProperty("totalExpense").GetDecimal().Should().Be(9m);

            var days = (await ReadJson(await DoGet("api/dashboard/daily-summary?year=2026&month=4" , _token)))
                .GetProperty("days").EnumerateArray().ToList();
            days[0].GetProperty("expense").GetDecimal().Should().Be(5m);
            days[29].GetProperty("expense").GetDecimal().Should().Be(70m);
            days[^1].GetProperty("cumulativeExpense").GetDecimal().Should().Be(75m);

            var byCategory = await ReadJson(await DoGet("api/dashboard/expenses-by-category?year=2026&month=4" , _token));
            byCategory.EnumerateArray().Sum(c => c.GetProperty("amount").GetDecimal()).Should().Be(75m);
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
