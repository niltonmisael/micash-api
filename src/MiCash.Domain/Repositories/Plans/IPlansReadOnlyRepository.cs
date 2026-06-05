using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Plans
{
    public interface IPlansReadOnlyRepository
    {
        Task<IEnumerable<Plan>> GetAllActiveAsync (CancellationToken cancellationToken);
        Task<Plan?> GetByIdAsync ( long planId, CancellationToken cancellationToken );
    }
}
