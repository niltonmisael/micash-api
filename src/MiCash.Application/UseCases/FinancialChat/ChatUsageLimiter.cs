using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Conversations;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialChat
{
    /// <summary>How many chat messages the user can still send in the plan's period.</summary>
    public sealed record ChatUsage ( string Period , int Limit , int Used , DateTime ResetsAt )
    {
        public int Remaining => Math.Max(Limit - Used , 0);
    }

    public interface IChatUsageLimiter
    {
        /// <summary>Throws PlanLimitException (Free, 403) or ChatRateLimitException (Premium, 429) when over the limit.</summary>
        Task EnsureCanSendAsync ( User user , CancellationToken cancellationToken );

        /// <summary>Usage in the plan's main period (month for Free, day for Premium); null when unlimited.</summary>
        Task<ChatUsage?> GetUsageAsync ( User user , CancellationToken cancellationToken );
    }

    /// <summary>
    /// Counts the user's own messages already saved (conversation_messages, role User), so no extra table.
    /// Periods follow the Brasília calendar: the day and the month reset at 00:00 in Brasília.
    /// A message is only saved when the AI answers, so failed attempts do not count.
    /// </summary>
    public sealed class ChatUsageLimiter : IChatUsageLimiter
    {
        public const string PERIOD_MONTH = "month";
        public const string PERIOD_DAY = "day";

        private readonly IUserPlanService _userPlanService;
        private readonly IConversationMessageReadOnlyRepository _messages;
        private readonly IBrazilClock _clock;

        public ChatUsageLimiter ( IUserPlanService userPlanService , IConversationMessageReadOnlyRepository messages , IBrazilClock clock )
        {
            _userPlanService = userPlanService;
            _messages = messages;
            _clock = clock;
        }

        public async Task EnsureCanSendAsync ( User user , CancellationToken cancellationToken )
        {
            var limits = await _userPlanService.GetLimitsAsync(user , cancellationToken);

            var usage = await GetUsageAsync(user , limits , cancellationToken);

            if ( usage is not null && usage.Remaining == 0 ) {
                if ( usage.Period == PERIOD_MONTH ) {
                    throw new PlanLimitException(string.Format(ResourceErrorMessages.PLAN_LIMIT_CHAT_MONTHLY , usage.Limit) , usage.ResetsAt);
                }

                throw new ChatRateLimitException(string.Format(ResourceErrorMessages.CHAT_DAILY_LIMIT , usage.Limit) , usage.ResetsAt);
            }

            if ( limits.ChatMessagesPerMinute is int perMinute ) {
                var now = _clock.UtcNow;
                var lastMinute = await _messages.CountUserMessagesSinceAsync(user.Id , now.AddMinutes(-1) , cancellationToken);

                if ( lastMinute >= perMinute ) {
                    throw new ChatRateLimitException(ResourceErrorMessages.CHAT_MINUTE_LIMIT , now.AddMinutes(1));
                }
            }
        }

        public async Task<ChatUsage?> GetUsageAsync ( User user , CancellationToken cancellationToken )
        {
            var limits = await _userPlanService.GetLimitsAsync(user , cancellationToken);

            return await GetUsageAsync(user , limits , cancellationToken);
        }

        private async Task<ChatUsage?> GetUsageAsync ( User user , PlanLimits limits , CancellationToken cancellationToken )
        {
            var today = _clock.Today;

            if ( limits.ChatMessagesPerMonth is int perMonth ) {
                var monthStart = new DateOnly(today.Year , today.Month , 1);
                var used = await _messages.CountUserMessagesSinceAsync(user.Id , _clock.StartOfDayUtc(monthStart) , cancellationToken);

                return new ChatUsage(PERIOD_MONTH , perMonth , used , _clock.StartOfDayUtc(monthStart.AddMonths(1)));
            }

            if ( limits.ChatMessagesPerDay is int perDay ) {
                var used = await _messages.CountUserMessagesSinceAsync(user.Id , _clock.StartOfDayUtc(today) , cancellationToken);

                return new ChatUsage(PERIOD_DAY , perDay , used , _clock.StartOfDayUtc(today.AddDays(1)));
            }

            return null;
        }
    }
}
