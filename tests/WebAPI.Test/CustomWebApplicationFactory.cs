using CommonTestUtilities.Cache;
using CommonTestUtilities.AI;
using MiCash.Domain.Services.IA;
using MiCash.Domain.Entities;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.Cache;
using MiCash.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Test
{
    public class CustomWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private string _token = string.Empty;

        private string _email = string.Empty;
        private string _name = string.Empty;
        private string _password = string.Empty;
        private DateTime _date = DateTime.Now;
        private long _expenseId;
        private long _categoryId;

        public string GetToken () => _token;

        public string GetEmail () => _email;

        public string GetName () => _name;

        public string GetPassword () => _password;

        public long GetExpenseId () => _expenseId;

        public long GetCategoryId () => _categoryId;

        public DateTime GetDate() => _date;

        protected override void ConfigureWebHost (
            IWebHostBuilder builder )
        {
            builder
                .UseEnvironment("Test")
                .ConfigureServices(services => {
                    services.AddScoped<IRedisCacheService ,
                        FakeRedisCacheService>();

                    services.AddScoped<IAIService , FakeAIService>(_ => new FakeAIService());

                    var provider = services
                        .AddEntityFrameworkInMemoryDatabase()
                        .BuildServiceProvider();

                    services.AddDbContext<MiCashDbContext>(options => {
                        options.UseInMemoryDatabase(
                            "InMemoryDbForTesting");

                        options.UseInternalServiceProvider(provider);
                    });

                    var scope = services
                        .BuildServiceProvider()
                        .CreateScope();

                    var dbContext = scope.ServiceProvider
                        .GetRequiredService<MiCashDbContext>();

                    var passwordEncripter = scope.ServiceProvider
                        .GetRequiredService<IPasswordEncripter>();

                    var accessTokenGenerator = scope.ServiceProvider
                        .GetRequiredService<IAccessTokenGenerator>();

                    StartDataBase(
                        dbContext ,
                        passwordEncripter ,
                        accessTokenGenerator);
                });
        }

        private void StartDataBase (
            MiCashDbContext dbContext ,
            IPasswordEncripter passwordEncripter ,
            IAccessTokenGenerator accessTokenGenerator )
        {
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            _name = "Usuário Teste";
            _email = "teste@micash.com";
            _password = "123456";

            var user = new User
            {
                Name = _name ,
                Email = _email ,
                Phone = "43999999999" ,
                PasswordHash = passwordEncripter.Encrypt(_password) ,
                UserIdentifier = Guid.NewGuid()
            };

            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            var category = new Category
            {
                Name = "Moradia" ,
                UserId = user.Id
            };

            dbContext.Categories.Add(category);
            dbContext.SaveChanges();

            _categoryId = category.Id;

            var expense = new Expense
            {
                UserId = user.Id ,
                CategoryId = category.Id ,
                Description = "Pagamento da conta de luz",
                Title = "Conta de Luz" ,
                Amount = 150 ,
                ExpenseDate = DateTime.Today
            };

            dbContext.Expenses.Add(expense);
            dbContext.SaveChanges();

            _expenseId = expense.Id;

            _date = expense.ExpenseDate;

            _token = accessTokenGenerator.Generate(user);
        }
    }
}