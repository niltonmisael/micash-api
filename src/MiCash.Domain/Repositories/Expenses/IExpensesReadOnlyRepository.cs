using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Expenses
{
    public interface IExpensesReadOnlyRepository
    {
        public Task<List<Expense>> GetAll (User user, CancellationToken cancellationToken);
        public Task<Expense?> GetById (User user ,long id , CancellationToken cancellationToken );
        public Task<List<Expense>> FilterByMonth(User user, DateOnly date, CancellationToken cancellationToken);
    }
}
