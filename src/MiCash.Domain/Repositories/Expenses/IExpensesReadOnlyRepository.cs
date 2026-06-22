using MiCash.Domain.Entities;
using MiCash.Domain.Services.Pagination;

namespace MiCash.Domain.Repositories.Expenses
{
    public interface IExpensesReadOnlyRepository
    {
        Task<PagedResult<Expense>> GetAllAsync (
            long userId ,
            int page ,
            int pageSize ,
            CancellationToken cancellationToken );

        public Task<Expense?> GetById (User user ,long id , CancellationToken cancellationToken );
        public Task<List<Expense>> FilterByMonth(User user, DateOnly date, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Expense>> GetByPeriodAsync (long userId , int month , int year , CancellationToken cancellationToken );
    }
}
