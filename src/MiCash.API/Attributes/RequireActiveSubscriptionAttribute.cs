using MiCash.API.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Attributes
{
    public class RequireActiveSubscriptionAttribute : TypeFilterAttribute
    {
        public RequireActiveSubscriptionAttribute () : base(typeof(RequireActiveSubscriptionFilter)){ }
    }
}
