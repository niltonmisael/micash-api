using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using FluentAssertions;
using MiCash.Application.UseCases.Expenses.GetAll;
using MiCash.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.Test.Expenses.GetAll
{
    public class GetAllExpenseUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var loggedUser = UserBuilder.Build();
            var expenses = ExpenseBuilder.Collection(loggedUser);

            var useCase = CreateUseCase(loggedUser, expenses);

            var result = await useCase.Execute();

            result.Should().NotBeNull();
            result.Expenses.Should().NotBeNullOrEmpty().And.AllSatisfy(expense => {

                expense.Id.Should().BeGreaterThan(0);
                expense.Title.Should().NotBeEmpty();
                expense.Amount.Should().BeGreaterThan(0);
            });
        }

        private GetAllExpensesUseCase CreateUseCase ( User user, List<Expense> expenses)
        {
            var repository = new ExpenseReadOnlyRepositoryBuilder().GetAll(user, expenses).Build();
            var mapper = MapperBuilder.Build();
            var loggedUser = LoggedUserBuilder.Build(user);

            return new GetAllExpensesUseCase(repository , mapper , loggedUser);
        }
    }
}
