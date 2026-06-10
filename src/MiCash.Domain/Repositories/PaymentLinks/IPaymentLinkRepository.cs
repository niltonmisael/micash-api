using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.PaymentLinks
{
    public interface IPaymentLinkRepository
    {
        Task AddAsync (
            PaymentLink paymentLink ,
            CancellationToken cancellationToken );

        Task<PaymentLink?> GetByPaymentLinkIdAsync (
            string paymentLinkId ,
            CancellationToken cancellationToken );

        Task<PaymentLink?> GetByOrderCodeAsync (
            string orderCode ,
            CancellationToken cancellationToken );

        Task<PaymentLink?> GetByPagarmePaymentLinkIdAsync (
            string paymentLinkId ,
            CancellationToken cancellationToken );
    }
}
