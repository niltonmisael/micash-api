using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Payments;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    internal class PaymentRepository : IPaymentCustomerRepository, IPaymentTransactionRepository
    {
        private readonly MiCashDbContext _dbContext;

        public PaymentRepository ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public Task<PaymentCustomer?> GetAsync ( long userId , string provider , string externalCustomerId , CancellationToken cancellationToken ) =>
            _dbContext.PaymentCustomers.FirstOrDefaultAsync(
                c => c.UserId == userId && c.Provider == provider && c.ExternalCustomerId == externalCustomerId ,
                cancellationToken);

        public async Task AddAsync ( PaymentCustomer customer , CancellationToken cancellationToken ) =>
            await _dbContext.PaymentCustomers.AddAsync(customer , cancellationToken);

        public Task<PaymentTransaction?> GetByExternalChargeIdAsync ( string externalChargeId , CancellationToken cancellationToken ) =>
            _dbContext.PaymentTransactions.FirstOrDefaultAsync(t => t.ExternalChargeId == externalChargeId , cancellationToken);

        public async Task AddAsync ( PaymentTransaction transaction , CancellationToken cancellationToken ) =>
            await _dbContext.PaymentTransactions.AddAsync(transaction , cancellationToken);
    }
}
