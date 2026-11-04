using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.PaymentLinks;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public class PaymentLinkRepository : IPaymentLinkRepository
    {
        private readonly MiCashDbContext _dbContext;

        public PaymentLinkRepository ( 
            MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddAsync ( PaymentLink paymentLink , CancellationToken cancellationToken )
        {
            await _dbContext.PaymentLinks.AddAsync ( paymentLink , cancellationToken );
        }

        public async Task<PaymentLink?> GetByOrderCodeAsync ( string orderCode , CancellationToken cancellationToken )
        {
            return await _dbContext.PaymentLinks.FirstOrDefaultAsync( p => p.OrderCode == orderCode , cancellationToken);
        }

        public Task<PaymentLink?> GetByPagarmePaymentLinkIdAsync ( string paymentLinkId , CancellationToken cancellationToken )
        {
            return _dbContext.PaymentLinks.FirstOrDefaultAsync( p => p.PagarmePaymentLinkId == paymentLinkId , cancellationToken );
        }
    }
}
