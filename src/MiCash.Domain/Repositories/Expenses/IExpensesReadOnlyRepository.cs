using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Expenses
{
    public interface IExpensesReadOnlyRepository
    {
        public Task<List<Expense>> GetAll (User user);
        public Task<Expense?> GetById (User user ,long id );
        public Task<List<Expense>> FilterByMonth(User user, DateOnly date);
    }
}
