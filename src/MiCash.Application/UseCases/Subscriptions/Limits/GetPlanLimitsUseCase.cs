using MiCash.Application.UseCases.FinancialChat;
using MiCash.Communication.Responses.Subscriptions;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Plans;

namespace MiCash.Application.UseCases.Subscriptions.Limits
{
    public interface IGetPlanLimitsUseCase
    {
        Task<ResponsePlanLimitsJson> Execute ( CancellationToken cancellationToken );
    }

    public sealed class GetPlanLimitsUseCase : IGetPlanLimitsUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserPlanService _userPlanService;
        private readonly IChatUsageLimiter _chatUsageLimiter;

        public GetPlanLimitsUseCase ( ILoggedUser loggedUser , IUserPlanService userPlanService , IChatUsageLimiter chatUsageLimiter )
        {
            _loggedUser = loggedUser;
            _userPlanService = userPlanService;
            _chatUsageLimiter = chatUsageLimiter;
        }

        public async Task<ResponsePlanLimitsJson> Execute ( CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var limits = await _userPlanService.GetLimitsAsync(user , cancellationToken);
            var chat = await _chatUsageLimiter.GetUsageAsync(user , cancellationToken);

            return new ResponsePlanLimitsJson
            {
                Plan = limits.Name ,
                IsPremium = limits.IsPremium ,
                MaxFinancialGoalsPerMonth = limits.MaxFinancialGoalsPerMonth ,
                MaxSavingsGoals = limits.MaxSavingsGoals ,
                HistoryMonths = limits.HistoryMonths ,
                FullInsights = limits.FullInsights ,
                GoalWarningAlerts = limits.GoalWarningAlerts ,
                CanCopyFinancialGoals = limits.CanCopyFinancialGoals ,
                MonthForecast = limits.MonthForecast ,
                Chat = new ResponseChatLimitJson
                {
                    Period = chat?.Period ?? string.Empty ,
                    Limit = chat?.Limit ?? 0 ,
                    Used = chat?.Used ?? 0 ,
                    Remaining = chat?.Remaining ?? 0 ,
                    ResetsAt = chat?.ResetsAt ?? default ,
                    PerMinute = limits.ChatMessagesPerMinute
                }
            };
        }
    }
}
