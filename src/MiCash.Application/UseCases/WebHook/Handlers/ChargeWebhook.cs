using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Payments;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    internal static class ChargeWebhook
    {
        /// <summary>Pagar.me subscription id of a charge event ("data"), or null for a one-off charge.</summary>
        public static string? GetSubscriptionId ( JsonElement data ) =>
            WebhookPayload.GetString(data , "invoice" , "subscriptionId")
            ?? WebhookPayload.GetString(data , "invoice" , "subscription_id")
            ?? WebhookPayload.GetString(data , "invoice" , "subscription" , "id");

        /// <summary>
        /// Upserts the charge ("data") as a transaction of <paramref name="subscription"/>, the one the gateway billed
        /// it for. Recorded even when the subscription is already closed: money moved anyway. A charge already
        /// recorded keeps its subscription (never moved to another one) and its first dates.
        /// </summary>
        public static async Task RecordAsync (
            IPaymentTransactionRepository transactions ,
            Subscription subscription ,
            JsonElement data ,
            PaymentTransactionStatus status ,
            DateTime now ,
            CancellationToken cancellationToken )
        {
            var chargeId = WebhookPayload.GetString(data , "id");

            if ( string.IsNullOrWhiteSpace(chargeId) ) {
                return;
            }

            var transaction = await transactions.GetByExternalChargeIdAsync(chargeId , cancellationToken);

            if ( transaction is null ) {
                transaction = new PaymentTransaction
                {
                    SubscriptionId = subscription.Id ,
                    ExternalChargeId = chargeId ,
                    CreatedAt = now
                };

                await transactions.AddAsync(transaction , cancellationToken);
            }

            transaction.AmountCents = WebhookPayload.GetLong(data , "amount") ?? transaction.AmountCents;
            transaction.Status = status;
            transaction.UpdatedAt = now;

            switch ( status ) {
                case PaymentTransactionStatus.Paid:
                    transaction.PaidAt ??= WebhookPayload.GetUtcDateTime(data , "paid_at") ?? now;
                    break;
                case PaymentTransactionStatus.Failed:
                    transaction.FailedAt ??= now;
                    break;
                default:
                    transaction.RefundedAt ??= WebhookPayload.GetUtcDateTime(data , "canceled_at") ?? now;
                    break;
            }
        }
    }
}
