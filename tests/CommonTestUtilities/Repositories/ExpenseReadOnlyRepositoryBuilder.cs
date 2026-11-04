using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Pagination;
using Moq;

namespace CommonTestUtilities.Repositories
{
    public class ExpenseReadOnlyRepositoryBuilder
    {
        private readonly Mock<IExpensesReadOnlyRepository> _repository;

        public ExpenseReadOnlyRepositoryBuilder ()
        {
            _repository = new Mock<IExpensesReadOnlyRepository>();
        }

        public ExpenseReadOnlyRepositoryBuilder GetAll (
            User user ,
            List<Expense> expenses ,
            int page = 1 ,
            int pageSize = 10 )
        {
            var pagedResult = new PagedResult<Expense>
            {
                Data = expenses ,
                TotalRecords = expenses.Count
            };

            _repository
                .Setup(repository =>
                    repository.GetAllAsync(
                        user.Id ,
                        page ,
                        pageSize ,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            return this;
        }
        public ExpenseReadOnlyRepositoryBuilder GetById ( User user , Expense? expense )
        {

            if ( expense is not null )
                _repository.Setup(repository => repository.GetById(user , expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

            return this;
        }

        public ExpenseReadOnlyRepositoryBuilder GetByPeriod ( User user , int month , int year , List<Expense> expenses )
        {
            _repository
                .Setup(repository => repository.GetByPeriodAsync(user.Id , month , year , It.IsAny<CancellationToken>()))
                .ReturnsAsync(expenses);

            return this;
        }

        public IExpensesReadOnlyRepository Build() => _repository.Object;
    }
}
