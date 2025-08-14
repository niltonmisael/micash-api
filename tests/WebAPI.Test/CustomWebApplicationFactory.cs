using CommonTestUtilities.Entities;
using MiCash.Domain.Entities;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAPI.Test
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private Expense _expense;
        private User _user;
        private string _password;
        private string _token;
        protected override void ConfigureWebHost ( IWebHostBuilder builder )
        {
            builder.UseEnvironment("Test")
                .ConfigureServices(services => {

                    var provider = services.AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();  

                    services.AddDbContext<MiCashDbContext>(config => {
                        config.UseInMemoryDatabase("InMemoryDbForTesting");
                        config.UseInternalServiceProvider(provider);
                    });

                    var scope = services.BuildServiceProvider().CreateScope();
                    var dbContex = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();
                    var passwordEncripter = scope.ServiceProvider.GetRequiredService<IPasswordEncripter>();
                    var tokenEncripter = scope.ServiceProvider.GetRequiredService<IAccessTokenGenerator>();

                    StartDataBase(dbContex , passwordEncripter);
                    _token = tokenEncripter.Generate(_user);
                });
        }
        public string GetName () => _user.Name;
        public string GetEmail () => _user.Email;
        public string GetPassword () => _password;
        public string GetToken () => _token;
        public long GetExpenseId () => _expense.Id;
        
        private void StartDataBase (MiCashDbContext dbContext, IPasswordEncripter passwordEncripter )
        {
            AddUsers( dbContext, passwordEncripter );
            AddExpenses(dbContext , _user);

            dbContext.SaveChanges();
        }
        
        private void AddUsers(MiCashDbContext dbContext, IPasswordEncripter passwordEncripter )
        {
            _user = UserBuilder.Build();
            _password = _user.Password;

            _user.Password = passwordEncripter.Encrypt(_user.Password);

            dbContext.Users.Add(_user);
        }

        private void AddExpenses(MiCashDbContext dbContext, User user )
        {
            var expense = ExpenseBuilder.Build(user);

            dbContext.Expenses.Add(expense);
        }
    }
   
}
