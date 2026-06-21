using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Incomes
{
    public interface IIncomesReadOnlyRepository
    {
        Task<IEnumerable<Income>> GetAllAsync ( User userLogged , CancellationToken cancellationToken );
        Task<Income?> GetByIdAsync ( long userId , long incomeId , CancellationToken cancellationToken );
        Task<IReadOnlyCollection<Income>> GetByPeriodAsync ( long userId , int month , int year , CancellationToken cancellationToken );
    }
}
