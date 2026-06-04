using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Incomes
{
    public interface IIncomesUpdateOnlyRepository
    {
        Task UpdateAsync ( Income income , CancellationToken cancellationToken );
    }
}
