using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Transactions
{
    public interface ITransactionsReadOnlyRepository
    {
        /// <summary>Expenses of the Brasília month (same rule as the dashboard), with their category.</summary>
        Task<IReadOnlyList<Expense>> GetExpensesOfMonthAsync ( long userId , int year , int month , CancellationToken cancellationToken );

        /// <summary>Incomes whose ReceivedAt falls in the month.</summary>
        Task<IReadOnlyList<Income>> GetIncomesOfMonthAsync ( long userId , int year , int month , CancellationToken cancellationToken );
    }
}
