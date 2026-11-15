namespace MiCash.Domain.Enums
{
    public enum PaymentTransactionStatus
    {
        Paid = 0,
        Failed = 1,
        Refunded = 2,
        Chargedback = 3,
    }
}
