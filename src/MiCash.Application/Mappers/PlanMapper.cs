using MiCash.Communication.Responses.Plans;
using MiCash.Domain.Entities;

namespace MiCash.Application.Mappers
{
    internal static class PlanMapper
    {
        public static ResponsePlanJson ToResponse ( this Plan plan ) => new()
        {
            Id = plan.Id ,
            Name = plan.Name ,
            Price = plan.Price ,
            BillingPeriod = plan.BillingPeriod.ToString()
        };

        public static ResponsePlansJson ToListItemResponse ( this Plan plan ) => new()
        {
            Id = plan.Id ,
            Name = plan.Name ,
            Price = plan.Price ,
            BillingPeriod = plan.BillingPeriod.ToString()
        };
    }
}
