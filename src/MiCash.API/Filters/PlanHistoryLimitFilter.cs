using MiCash.Communication.Responses;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCash.API.Filters
{
    /// <summary>
    /// Free plan: the dashboard and goal screens only show the current month, the previous HistoryMonths ones
    /// and future months. Reads the "year" and "month" query parameters; when missing or invalid, the use case
    /// validation answers. The data is never hidden from the expense/income lists.
    /// </summary>
    public class PlanHistoryLimitFilter : IAsyncActionFilter
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserPlanService _userPlanService;
        private readonly IBrazilClock _clock;

        public PlanHistoryLimitFilter ( ILoggedUser loggedUser , IUserPlanService userPlanService , IBrazilClock clock )
        {
            _loggedUser = loggedUser;
            _userPlanService = userPlanService;
            _clock = clock;
        }

        public async Task OnActionExecutionAsync ( ActionExecutingContext context , ActionExecutionDelegate next )
        {
            var query = context.HttpContext.Request.Query;

            if ( int.TryParse(query["year"] , out var year) && int.TryParse(query["month"] , out var month) && month is >= 1 and <= 12 ) {
                var user = await _loggedUser.Get();
                var limits = await _userPlanService.GetLimitsAsync(user , context.HttpContext.RequestAborted);

                if ( limits.AllowsMonth(_clock.Today , year , month) == false ) {
                    // Expected flow (e.g. the app's 6-month evolution chart asks for blocked months), so no
                    // exception: same body ExceptionFilter writes for PlanLimitException, without the throw.
                    context.Result = new ObjectResult(new ResponseErrorJson(string.Format(ResourceErrorMessages.PLAN_LIMIT_HISTORY , limits.HistoryMonths))
                    {
                        ErrorCode = PlanLimitException.CODE
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };

                    return;
                }
            }

            await next();
        }
    }

    /// <summary>Applies <see cref="PlanHistoryLimitFilter"/> to an action with year/month query parameters.</summary>
    public class PlanHistoryLimitAttribute : TypeFilterAttribute
    {
        public PlanHistoryLimitAttribute () : base(typeof(PlanHistoryLimitFilter)) { }
    }
}
