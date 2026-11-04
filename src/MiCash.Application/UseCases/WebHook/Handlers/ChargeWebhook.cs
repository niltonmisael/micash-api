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
    }
}
