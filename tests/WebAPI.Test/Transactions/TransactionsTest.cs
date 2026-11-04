using FluentAssertions;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Transactions
{
    public class TransactionsTest : MiCashClassFixture
    {
        // A fixed month that no other test seeds (September 2025).
        private const string MONTH = "year=2025&month=9";

        private readonly CustomWebApplicationFactory _factory;
        private readonly string _token;

        public TransactionsTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
            _token = factory.GetToken();
        }

        [Fact]
        public async Task Lists_The_Month_Merged_And_Matching_The_Monthly_Summary ()
        {
            await SeedOnce();

            var page = await ReadJson(await DoGet($"api/transactions?{MONTH}&pageSize=50" , _token));
            var items = page.GetProperty("data").EnumerateArray().ToList();

            // August 31st 23:59 in Brasília (UTC September 1st 02:59) stays out of September.
            items.Select(i => i.GetProperty("title").GetString())
                .Should().Equal("Fim do mês" , "Freela" , "Mercado" , "Salário" , "Início");
            page.GetProperty("totalRecords").GetInt32().Should().Be(5);

            var last = items[0];
            last.GetProperty("type").GetInt32().Should().Be(2);
            last.GetProperty("date").GetString().Should().Be("2025-09-30");
            last.GetProperty("categoryName").GetString().Should().Be("Moradia");

            var monthly = await ReadJson(await DoGet($"api/dashboard/monthly-summary?{MONTH}" , _token));
            var totalExpense = items.Where(i => i.GetProperty("type").GetInt32() == 2).Sum(i => i.GetProperty("amount").GetDecimal());
            var totalIncome = items.Where(i => i.GetProperty("type").GetInt32() == 1).Sum(i => i.GetProperty("amount").GetDecimal());
            totalExpense.Should().Be(monthly.GetProperty("totalExpense").GetDecimal());
            totalIncome.Should().Be(monthly.GetProperty("totalIncome").GetDecimal());
        }

        [Fact]
        public async Task Filters_By_Type_Income_Category_And_Search ()
        {
            await SeedOnce();

            var incomes = await ReadJson(await DoGet($"api/transactions?{MONTH}&type=1" , _token));
            incomes.GetProperty("data").EnumerateArray().Should().OnlyContain(i => i.GetProperty("type").GetInt32() == 1)
                .And.HaveCount(2);

            var freelance = await ReadJson(await DoGet($"api/transactions?{MONTH}&incomeCategory=2" , _token));
            freelance.GetProperty("data").EnumerateArray().Select(i => i.GetProperty("title").GetString()).Should().Equal("Freela");

            var search = await ReadJson(await DoGet($"api/transactions?{MONTH}&search=MERCADO" , _token));
            search.GetProperty("data").EnumerateArray().Select(i => i.GetProperty("title").GetString()).Should().Equal("Mercado");
        }

        private static readonly SemaphoreSlim SeedLock = new(1 , 1);

        private async Task SeedOnce ()
        {
            await SeedLock.WaitAsync();
            try {
                await using var scope = _factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();
                var userId = await db.Users.Where(u => u.Email == _factory.GetEmail()).Select(u => u.Id).SingleAsync();

                // The in-memory database is shared by the tests of this class: seed it only once.
                if ( await db.Incomes.AnyAsync(i => i.UserId == userId && i.Title == "Freela" && i.ReceivedAt == new DateOnly(2025 , 9 , 20)) ) {
                    return;
                }
                var categoryId = _factory.GetCategoryId();

                db.Incomes.AddRange(
                    new Income { UserId = userId , Title = "Salário" , Amount = 3000m , ReceivedAt = new DateOnly(2025 , 9 , 5) , Category = IncomeCategory.Salary } ,
                    new Income { UserId = userId , Title = "Freela" , Amount = 800m , ReceivedAt = new DateOnly(2025 , 9 , 20) , Category = IncomeCategory.Freelance });

                db.Expenses.AddRange(
                    new Expense { UserId = userId , CategoryId = categoryId , Title = "Agosto" , Amount = 1m , ExpenseDate = new DateTime(2025 , 9 , 1 , 2 , 59 , 0 , DateTimeKind.Utc) } ,
                    new Expense { UserId = userId , CategoryId = categoryId , Title = "Início" , Amount = 5m , ExpenseDate = new DateTime(2025 , 9 , 1 , 3 , 0 , 0 , DateTimeKind.Utc) } ,
                    new Expense { UserId = userId , CategoryId = categoryId , Title = "Mercado" , Amount = 250m , ExpenseDate = new DateTime(2025 , 9 , 12 , 15 , 0 , 0 , DateTimeKind.Utc) } ,
                    // 22:30 on September 30th in Brasília (already October 1st in UTC).
                    new Expense { UserId = userId , CategoryId = categoryId , Title = "Fim do mês" , Amount = 70m , ExpenseDate = new DateTime(2025 , 10 , 1 , 1 , 30 , 0 , DateTimeKind.Utc) });

                await db.SaveChangesAsync();
            }
            finally {
                SeedLock.Release();
            }
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
