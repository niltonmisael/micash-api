using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using FluentAssertions;
using MiCash.Application.UseCases.Expenses.GetAll;
using MiCash.Domain.Entities;

namespace UseCases.Test.Expenses.GetAll
{
    public class GetAllExpenseUseCaseTest
    {
        //[Fact]
        //public async Task Success ()
        //{
        //    var cancellationToken = CancellationToken.None;
        //    var loggedUser = UserBuilder.Build();
        //    var expenses = ExpenseBuilder.Collection(loggedUser);

        //    var useCase = CreateUseCase(loggedUser, expenses);

        //    var result = await useCase.Execute(cancellationToken);

        //    result.Should().NotBeNull();
        //    result.Expenses.Should().NotBeNullOrEmpty().And.AllSatisfy(expense => {

        //        expense.Id.Should().BeGreaterThan(0);
        //        expense.Title.Should().NotBeEmpty();
        //        expense.Amount.Should().BeGreaterThan(0);
        //    });
        //}

        //private GetAllExpensesUseCase CreateUseCase ( User user, List<Expense> expenses)
        //{
        //    var repository = new ExpenseReadOnlyRepositoryBuilder().GetAll(user, expenses).Build();
        //    var loggedUser = LoggedUserBuilder.Build(user);

        //    return new GetAllExpensesUseCase(repository , loggedUser);
        //}
    }
}
