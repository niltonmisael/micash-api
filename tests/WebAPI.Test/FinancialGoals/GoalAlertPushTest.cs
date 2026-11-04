using FluentAssertions;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace WebAPI.Test.FinancialGoals
{
    /// <summary>Registering expenses crosses the category goal: alert recorded in the notification center (and pushed).</summary>
    public class GoalAlertPushTest : MiCashClassFixture
    {
        private const string EXPENSE = "api/expense";
        private const string WARNING = "goal-warning";
        private const string EXCEEDED = "goal-exceeded";

        private readonly CustomWebApplicationFactory _factory;

        public GoalAlertPushTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Premium_Gets_80_And_100_Percent_Alerts_Once_Each ()
        {
            var token = _factory.GetToken();
            var (userId, categoryId) = await SeedGoal(_factory.GetEmail() , "Mercado Premium" , target: 100m);

            await RegisterExpense(token , categoryId , 50m);
            (await Alerts(userId , categoryId)).Should().BeEmpty("50% does not alert");

            await RegisterExpense(token , categoryId , 35m);
            (await Alerts(userId , categoryId)).Select(a => a.Type).Should().Equal(WARNING);

            await RegisterExpense(token , categoryId , 20m);
            await RegisterExpense(token , categoryId , 10m);

            var alerts = await Alerts(userId , categoryId);
            alerts.Select(a => a.Type).Should().Equal(WARNING , EXCEEDED);
            alerts[1].Title.Should().Be("Meta de Mercado Premium estourada");
            alerts[1].Data.Should().Contain("/goals?tab=limits");
        }

        [Fact]
        public async Task Free_Gets_Only_The_Exceeded_Alert ()
        {
            var token = _factory.GetTokenWithoutSubscription();
            var (userId, categoryId) = await SeedGoal("sem.assinatura@micash.com" , "Mercado Free" , target: 100m);

            await RegisterExpense(token , categoryId , 90m);
            (await Alerts(userId , categoryId)).Should().BeEmpty("the 80% warning is Premium");

            await RegisterExpense(token , categoryId , 15m);
            (await Alerts(userId , categoryId)).Select(a => a.Type).Should().Equal(EXCEEDED);
        }

        private async Task RegisterExpense ( string token , long categoryId , decimal amount )
        {
            var response = await DoPost(EXPENSE , new
            {
                Title = "Compra" ,
                Description = "Teste" ,
                ExpenseDate = DateTime.UtcNow.AddMinutes(-1) ,
                Amount = amount ,
                PaymentType = 0 ,
                ExpenseStatus = 0 ,
                CategoryId = categoryId
            } , token);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        /// <summary>A category of its own (no other test touches it) with a goal for the current Brasília month.</summary>
        private async Task<(long UserId, long CategoryId)> SeedGoal ( string email , string categoryName , decimal target )
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();

            var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow , TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);

            var category = new Category { UserId = userId , Name = categoryName , Color = "#F58A3C" , Icon = "shopping-cart" };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            db.FinancialGoals.Add(new FinancialGoal { UserId = userId , CategoryId = category.Id , TargetAmount = target , Month = today.Month , Year = today.Year });
            await db.SaveChangesAsync();

            return (userId, category.Id);
        }

        private async Task<List<PushNotification>> Alerts ( long userId , long categoryId )
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();

            var goalId = await db.FinancialGoals.Where(g => g.CategoryId == categoryId).Select(g => g.Id).SingleAsync();

            return await db.PushNotifications
                .Where(n => n.UserId == userId && n.IdempotencyKey != null && n.IdempotencyKey.Contains($":{goalId}:"))
                .OrderBy(n => n.Id)
                .ToListAsync();
        }
    }
}
