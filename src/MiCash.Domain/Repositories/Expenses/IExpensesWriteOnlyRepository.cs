using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Expenses
{
    public interface IExpensesWriteOnlyRepository
    {
        public Task Add ( Expense expense, CancellationToken cancellationToken );
       
        public Task Delete ( long id , CancellationToken cancellationToken);
    }
}
