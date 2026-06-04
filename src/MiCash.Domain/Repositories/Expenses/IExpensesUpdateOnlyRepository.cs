using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Expenses
{
    public interface IExpensesUpdateOnlyRepository
    {
        Task<Expense?> GetById(User user, long id, CancellationToken cancellationToken);
        void Update(Expense expense, CancellationToken cancellationToken);
    }
}
