using MiCash.Domain.Entities;
using MiCash.Domain.Services.Pagination;

namespace MiCash.Domain.Repositories.Incomes
{
    public interface IIncomesReadOnlyRepository
    {
        Task<PagedResult<Income>> GetAllAsync ( User userLogged , int page , int pageSize , CancellationToken cancellationToken );
        Task<Income?> GetByIdAsync ( long userId , long incomeId , CancellationToken cancellationToken );
        Task<IReadOnlyCollection<Income>> GetByPeriodAsync ( long userId , int month , int year , CancellationToken cancellationToken );
    }
}
