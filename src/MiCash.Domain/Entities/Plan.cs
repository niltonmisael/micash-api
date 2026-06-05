namespace MiCash.Domain.Entities
{
    public class Plan
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public string PagarmePlanId { get; set; } = string.Empty;
        public ICollection<Subscription> Subscriptions { get; set; } = [];
    }
}
