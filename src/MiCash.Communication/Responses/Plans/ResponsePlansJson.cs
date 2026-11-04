namespace MiCash.Communication.Responses.Plans
{
    public class ResponsePlansJson
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        /// <summary>"Monthly" or "Yearly".</summary>
        public string BillingPeriod { get; set; } = string.Empty;
    }
}
