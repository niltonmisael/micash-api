namespace MiCash.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public long UserId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PagarmeTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
