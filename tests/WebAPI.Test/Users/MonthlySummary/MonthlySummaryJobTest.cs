using FluentAssertions;
using MiCash.Application.Jobs.Users;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Jobs;
using MiCash.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Test.Users.MonthlySummary
{
    public class MonthlySummaryJobTest : MiCashClassFixture
    {
        // A month no other test seeds.
        private const int YEAR = 2025;
        private const int MONTH = 6;

        private readonly CustomWebApplicationFactory _factory;

        public MonthlySummaryJobTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Sends_Email_And_Push_Once ()
        {
            var user = await CreateUser("resumo.mensal@micash.com" , withEntries: true);

            await RunJob(user.Id);
            await RunJob(user.Id); // rerun (e.g. Hangfire retry): nothing new

            var emails = _factory.Emails.Sent.Where(e => e.To == user.Email).ToList();
            emails.Should().ContainSingle();
            emails[0].IdempotencyKey.Should().Be($"monthly-summary-{user.Id}-2025-06");
            emails[0].HtmlBody.Should().Contain("R$ 3.000,00").And.Contain("Mercado");

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();
            var pushes = await db.PushNotifications.Where(p => p.UserId == user.Id && p.Type == MonthlySummaryJob.PUSH_TYPE).ToListAsync();
            pushes.Should().ContainSingle();
            pushes[0].IdempotencyKey.Should().Be($"monthly-summary:{user.Id}:2025-06");

            (await db.Users.SingleAsync(u => u.Id == user.Id)).MonthlySummarySentFor.Should().Be(new DateOnly(YEAR , MONTH , 1));
        }

        [Fact]
        public async Task Dispatch_Enqueues_Only_Verified_Users_That_Want_It ()
        {
            var wants = await CreateUser("quer.resumo@micash.com" , withEntries: false);
            var optedOut = await CreateUser("nao.quer@micash.com" , withEntries: false , email: false , push: false);
            var unverified = await CreateUser("nao.verificado@micash.com" , withEntries: false , verified: false);

            await using ( var scope = _factory.Services.CreateAsyncScope() ) {
                await scope.ServiceProvider.GetRequiredService<IMonthlySummaryDispatchJob>().ExecuteAsync(CancellationToken.None);
            }

            var month = MiCash.Application.Jobs.Users.MonthlySummary.PreviousMonth(
                DateOnly.FromDateTime(BrazilClock.ToBrasilia(DateTime.UtcNow)));
            var enqueued = _factory.BackgroundJobs.EnqueuedMonthlySummaries.ToList();
            enqueued.Should().Contain((wants.Id, month.Year, month.Month));
            enqueued.Should().NotContain(e => e.UserId == optedOut.Id || e.UserId == unverified.Id);
        }

        private async Task RunJob ( long userId )
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IMonthlySummaryJob>().ExecuteAsync(userId , YEAR , MONTH , CancellationToken.None);
        }

        private async Task<User> CreateUser ( string address , bool withEntries , bool email = true , bool push = true , bool verified = true )
        {
            var wantsEmail = email;
            var wantsPush = push;
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();

            var user = new User
            {
                Name = "Resumo Teste" ,
                Email = address ,
                PasswordHash = "hash" ,
                EmailVerifiedAt = verified ? DateTime.UtcNow : null ,
                MonthlySummaryEmail = wantsEmail ,
                MonthlySummaryPush = wantsPush
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            if ( withEntries ) {
                var category = new Category { Name = "Mercado" , Color = "#F58A3C" , Icon = "shopping-cart" , UserId = user.Id };
                db.Categories.Add(category);
                await db.SaveChangesAsync();

                db.Incomes.Add(new Income { UserId = user.Id , Title = "Salário" , Amount = 3000m , ReceivedAt = new DateOnly(YEAR , MONTH , 5) , Category = IncomeCategory.Salary });
                db.Expenses.Add(new Expense { UserId = user.Id , CategoryId = category.Id , Title = "Supermercado" , Amount = 450m , ExpenseDate = new DateTime(YEAR , MONTH , 10 , 15 , 0 , 0 , DateTimeKind.Utc) });
                await db.SaveChangesAsync();
            }

            return user;
        }
    }
}
