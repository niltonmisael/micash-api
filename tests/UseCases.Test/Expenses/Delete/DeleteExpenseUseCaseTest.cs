using CommonTestUtilities.Cache;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using FluentAssertions;
using MiCash.Application.UseCases.Expenses.Delete;
using MiCash.Domain.Entities;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace UseCases.Test.Expenses.Delete
{
    public class DeleteExpenseUseCaseTest
    {

        [Fact]
        public async Task Success ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();
            var expense = ExpenseBuilder.Build(loggedUser);

            var useCase = CreateUseCase(loggedUser, expense);

            var act = async () => await useCase.Execute(expense.Id, cancellationToken);

            await act.Should().NotThrowAsync();

        }

        [Fact]
        public async Task Error_Expense_Not_Found ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();

            var useCase = CreateUseCase(loggedUser);

            var act = async () => await useCase.Execute(id: 10000, cancellationToken);

            var result = await act.Should().ThrowAsync<NotFoundException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourceErrorMessages.EXPENSE_NOT_FOUND));
        }

        private DeleteExpenseUseCase CreateUseCase (User user, Expense? expense = null )
        {
            var repositoryWriteOnly = ExpenseWriteOnlyRepositoryBuilder.Build();
            var repository = new ExpenseReadOnlyRepositoryBuilder().GetById(user, expense).Build();
            var unityOfWork = UnitOfWorkBuilder.Build();
            var loggedUser = LoggedUserBuilder.Build(user);
            var cacheService = new RedisCacheServiceBuilder().Build();
            var cacheInvalidator = new FinancialCacheInvalidatorBuilder().Build();

            return new DeleteExpenseUseCase( repositoryWriteOnly, unityOfWork, loggedUser, repository,cacheInvalidator, cacheService);   
        }
    }
}
