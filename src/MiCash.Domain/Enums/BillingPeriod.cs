namespace MiCash.Domain.Enums
{
    /// <summary>How often a plan is charged. Must match the interval of the plan registered at Pagar.me.</summary>
    public enum BillingPeriod
    {
        Monthly = 0,
        Yearly = 1
    }
}
