using MiCash.Domain.Enums;

namespace MiCash.Domain.Services.FinancialInsights
{
    public class FinancialInsight
    {
        public FinancialInsightType Type { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
