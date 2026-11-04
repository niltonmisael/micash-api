using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.FinancialGoals;
using MiCash.Domain.Services.Plans;
using MiCash.Domain.Services.PushNotifications;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MiCash.Application.Services.FinancialGoals
{
    public interface IFinancialGoalAlertNotifier
    {
        /// <summary>
        /// Call after an expense was saved: when its category goal (current Brasília month) reaches 80% (Premium)
        /// or 100% (every plan), the user gets a push + notification center entry. Never throws.
        /// </summary>
        Task NotifyAsync ( User user , long categoryId , DateTime expenseDateUtc , CancellationToken cancellationToken );
    }

    /// <summary>
    /// Same thresholds as the goals screen (FinancialGoalCalculator) and same plan rule as the alerts route
    /// (GoalWarningAlerts). One alert per goal and target: raising the target arms the alerts again.
    /// </summary>
    public sealed class FinancialGoalAlertNotifier : IFinancialGoalAlertNotifier
    {
        public const string TYPE_WARNING = "goal-warning";
        public const string TYPE_EXCEEDED = "goal-exceeded";

        /// <summary>Goals screen opened on the spending limits tab.</summary>
        public const string ROUTE = "/goals?tab=limits";

        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private readonly IFinancialGoalReadOnlyRepository _goals;
        private readonly IExpensesReadOnlyRepository _expenses;
        private readonly IFinancialGoalCalculator _calculator;
        private readonly IUserPlanService _userPlanService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IBrazilClock _clock;
        private readonly ILogger<FinancialGoalAlertNotifier> _logger;

        public FinancialGoalAlertNotifier (
            IFinancialGoalReadOnlyRepository goals ,
            IExpensesReadOnlyRepository expenses ,
            IFinancialGoalCalculator calculator ,
            IUserPlanService userPlanService ,
            IPushNotificationService pushNotificationService ,
            IBrazilClock clock ,
            ILogger<FinancialGoalAlertNotifier> logger )
        {
            _goals = goals;
            _expenses = expenses;
            _calculator = calculator;
            _userPlanService = userPlanService;
            _pushNotificationService = pushNotificationService;
            _clock = clock;
            _logger = logger;
        }

        public async Task NotifyAsync ( User user , long categoryId , DateTime expenseDateUtc , CancellationToken cancellationToken )
        {
            try {
                await NotifyCoreAsync(user , categoryId , expenseDateUtc , cancellationToken);
            } catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
                // Request aborted after the expense was saved: nothing to undo.
            } catch ( System.Exception ex ) {
                // The expense is already saved; an alert failure must never turn it into an error.
                _logger.LogWarning(ex , "Alerta de meta não enviado ao usuário {UserId} (categoria {CategoryId})." , user.Id , categoryId);
            }
        }

        private async Task NotifyCoreAsync ( User user , long categoryId , DateTime expenseDateUtc , CancellationToken cancellationToken )
        {
            // A backdated expense in a past month does not alert: that month is closed.
            var expenseDate = BrazilClock.ToBrasilia(expenseDateUtc);
            var today = _clock.Today;

            if ( expenseDate.Year != today.Year || expenseDate.Month != today.Month ) {
                return;
            }

            // DbContext is not thread-safe: queries run sequentially.
            var goals = await _goals.GetByPeriodAsync(user.Id , today.Month , today.Year , cancellationToken);
            var goal = goals.FirstOrDefault(g => g.CategoryId == categoryId);

            if ( goal is null ) {
                return;
            }

            var expenses = await _expenses.GetByPeriodAsync(user.Id , today.Month , today.Year , cancellationToken);
            var spent = expenses.Where(e => e.CategoryId == categoryId).Sum(e => e.Amount);

            var calculation = _calculator.Calculate(goal , spent);

            var message = calculation.Status switch {
                FinancialGoalProgressStatus.Exceeded => Exceeded(goal , calculation) ,
                FinancialGoalProgressStatus.Warning when ( await _userPlanService.GetLimitsAsync(user , cancellationToken) ).GoalWarningAlerts
                    => Warning(goal , calculation) ,
                _ => null
            };

            if ( message is not null ) {
                await _pushNotificationService.EnqueueAsync(user.Id , message , cancellationToken);
            }
        }

        private static PushMessage Warning ( FinancialGoal goal , FinancialGoalCalculation calculation ) => new(
            TYPE_WARNING ,
            $"Meta de {goal.Category.Name} quase no limite" ,
            $"Você já usou {Math.Floor(calculation.PercentageUsed):0}% da meta de {goal.Category.Name} deste mês " +
            $"({Money(calculation.CurrentAmount)} de {Money(calculation.TargetAmount)})." ,
            ROUTE ,
            Key(TYPE_WARNING , goal));

        private static PushMessage Exceeded ( FinancialGoal goal , FinancialGoalCalculation calculation ) => new(
            TYPE_EXCEEDED ,
            $"Meta de {goal.Category.Name} estourada" ,
            calculation.RemainingAmount == 0
                ? $"Você atingiu os {Money(calculation.TargetAmount)} da meta de {goal.Category.Name} deste mês."
                : $"Você passou {Money(-calculation.RemainingAmount)} da meta de {goal.Category.Name} deste mês " +
                  $"({Money(calculation.CurrentAmount)} de {Money(calculation.TargetAmount)})." ,
            ROUTE ,
            Key(TYPE_EXCEEDED , goal));

        /// <summary>Target in cents in the key: raising (or lowering) it allows a new alert for the same goal.</summary>
        private static string Key ( string type , FinancialGoal goal ) =>
            $"{type}:{goal.Id}:{decimal.ToInt64(decimal.Round(goal.TargetAmount * 100))}";

        private static string Money ( decimal value ) => value.ToString("C" , PtBr);
    }
}
