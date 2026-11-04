using System.Net;

namespace MiCash.Infrastructure.Services.Payments
{
    /// <summary>Unexpected answer from the payment gateway. Not a MiCashException: the client gets a generic 500.</summary>
    public sealed class PaymentGatewayException : System.Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public PaymentGatewayException ( string message , HttpStatusCode? statusCode = null )
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
