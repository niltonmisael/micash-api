using CommonTestUtilities.Entities;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Test.Resources;

namespace WebAPI.Test
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public ExpenseIdentityManager Expense_MemberTeam { get; private set; } = default!;
        public ExpenseIdentityManager Expense_Admin { get; private set; } = default!;
        public UserIdentityManager User_Team_Member { get; private set; } = default!;
        public UserIdentityManager User_Admin { get; private set; } = default!;
        
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
                    var accessTokenGenerator = scope.ServiceProvider.GetService<IAccessTokenGenerator>();

                    StartDataBase(dbContex , passwordEncripter, accessTokenGenerator!);

                    
                });
        }
        
        
        private void StartDataBase (
            MiCashDbContext dbContext, 
            IPasswordEncripter passwordEncripter, 
            IAccessTokenGenerator accessTokenGenerator )
        {
            var userTeamMember = AddUserTeamMember( dbContext, passwordEncripter, accessTokenGenerator  );
            var expenseTeamMember = AddExpenses(dbContext , userTeamMember, expenseId: 1, tagId: 1);
            Expense_MemberTeam = new ExpenseIdentityManager(expenseTeamMember);

            var userAdmin = AddUserAdmin(dbContext , passwordEncripter , accessTokenGenerator);
            var expenseAdmin = AddExpenses(dbContext , userAdmin, expenseId: 2, tagId: 2);
            Expense_Admin = new ExpenseIdentityManager(expenseAdmin);

            dbContext.SaveChanges();
        }
        
        private User AddUserTeamMember(
            MiCashDbContext dbContext, 
            IPasswordEncripter passwordEncripter, 
            IAccessTokenGenerator accessTokenGenerator )
        {
            var user = UserBuilder.Build();
            user.Id = 1;

            var password = user.Password;

            user.Password = passwordEncripter.Encrypt(user.Password);

            dbContext.Users.Add(user);

            var token = accessTokenGenerator.Generate(user);

            User_Team_Member = new UserIdentityManager(user, password, token);

            return user;
        }

        private User AddUserAdmin (
            MiCashDbContext dbContext ,
            IPasswordEncripter passwordEncripter ,
            IAccessTokenGenerator accessTokenGenerator )
        {
            var user = UserBuilder.Build(Roles.ADMIN);
            user.Id = 2;

            var password = user.Password;

            user.Password = passwordEncripter.Encrypt(user.Password);

            dbContext.Users.Add(user);

            var token = accessTokenGenerator.Generate(user);

            User_Admin = new UserIdentityManager(user , password , token);

            return user;
        }

        private Expense AddExpenses(MiCashDbContext dbContext, User user, long expenseId, long tagId )
        {
            var expense = ExpenseBuilder.Build(user);
            expense.Id = expenseId;

            foreach ( var tag in expense.Tags ) 
            {
                tag.Id = tagId;
                tag.ExpenseId = expenseId;
            }

            dbContext.Expenses.Add(expense);

            return expense;
        }
    }
   
}
