using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Payments
{
    /// <summary>Tracked entities; saved by <see cref="IUnitOfWork.Commit"/>.</summary>
    public interface IPaymentCustomerRepository
    {
        Task<PaymentCustomer?> GetAsync ( long userId , string provider , string externalCustomerId , CancellationToken cancellationToken );

        Task AddAsync ( PaymentCustomer customer , CancellationToken cancellationToken );
    }

    /// <summary>Tracked entities; saved by <see cref="IUnitOfWork.Commit"/>.</summary>
    public interface IPaymentTransactionRepository
    {
        Task<PaymentTransaction?> GetByExternalChargeIdAsync ( string externalChargeId , CancellationToken cancellationToken );

        Task AddAsync ( PaymentTransaction transaction , CancellationToken cancellationToken );
    }
}
