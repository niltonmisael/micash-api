using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Incomes
{
    public interface IIncomesWriteOnlyRepository
    {
        Task Add ( Income income , CancellationToken cancellationToken );
        Task DeleteAsync ( long incomeId , CancellationToken cancellationToken );
    }
}
