namespace MiCash.Application.UseCases.WebHook
{
    /// <summary>
    /// The event depends on data that has not arrived yet (Pagar.me does not guarantee delivery order, e.g.
    /// charge.paid before subscription.created). The webhook answers non-2xx so Pagar.me delivers it again.
    /// </summary>
    public sealed class WebhookNotReadyException : System.Exception
    {
        public WebhookNotReadyException ( string message ) : base(message) { }
    }
}
