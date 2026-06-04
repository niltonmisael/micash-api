using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Expenses;
using Moq;

namespace CommonTestUtilities.Repositories
{
    public class ExpensesUpdateOnlyRepositoryBuilder
    {
        private readonly Mock<IExpensesUpdateOnlyRepository> _repository;

        public ExpensesUpdateOnlyRepositoryBuilder ()
        {
            _repository = new Mock<IExpensesUpdateOnlyRepository>();
        }

        public ExpensesUpdateOnlyRepositoryBuilder GetById ( User user , Expense? expense )
        {
            if ( expense is not null )
                _repository.Setup(repository => repository.GetById(user , expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

            return this;
        }
        public IExpensesUpdateOnlyRepository Build () => _repository.Object;
    }
}
