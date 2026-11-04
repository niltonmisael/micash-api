using MiCash.Application.Jobs.Email;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Repositories.Transactions;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Email;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.PushNotifications;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MiCash.Application.Jobs.Users
{
    /// <summary>
    /// Day 1 (Brasília): finds the verified users that want the summary (e-mail or push) and did not get last
    /// month's yet, and enqueues one job per user, in pages, so a failure only retries that user.
    /// </summary>
    public sealed class MonthlySummaryDispatchJob : IMonthlySummaryDispatchJob
    {
        public const int PAGE_SIZE = 500;

        private readonly IUserReadOnlyRepository _users;
        private readonly IBackgroundJobService _jobs;
        private readonly IBrazilClock _clock;
        private readonly ILogger<MonthlySummaryDispatchJob> _logger;

        public MonthlySummaryDispatchJob (
            IUserReadOnlyRepository users ,
            IBackgroundJobService jobs ,
            IBrazilClock clock ,
            ILogger<MonthlySummaryDispatchJob> logger )
        {
            _users = users;
            _jobs = jobs;
            _clock = clock;
            _logger = logger;
        }

        public async Task ExecuteAsync ( CancellationToken cancellationToken )
        {
            var month = MonthlySummary.PreviousMonth(_clock.Today);
            long afterId = 0;
            var total = 0;

            while ( true ) {
                var ids = await _users.GetMonthlySummaryRecipientIdsAsync(month , afterId , PAGE_SIZE , cancellationToken);
                foreach ( var id in ids ) {
                    _jobs.EnqueueMonthlySummary(id , month.Year , month.Month);
                }
                total += ids.Count;
                if ( ids.Count < PAGE_SIZE ) {
                    break;
                }
                afterId = ids[^1];
            }

            _logger.LogInformation("Monthly summary of {Month:yyyy-MM} enqueued for {Count} user(s)." , month , total);
        }
    }

    /// <summary>
    /// The summary of one user for one month: e-mail template 05 and/or a short push opening the reports.
    /// Same month rules as the dashboard (expenses by the Brasília month, incomes by ReceivedAt). A month with
    /// no entries sends nothing. Marks <c>MonthlySummarySentFor</c> so a rerun never sends twice.
    /// </summary>
    public sealed class MonthlySummaryJob : IMonthlySummaryJob
    {
        public const string PUSH_TYPE = "monthly-summary";

        /// <summary>Reports screen of the mobile app.</summary>
        public const string ROUTE = "/reports";

        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private readonly IUserReadOnlyRepository _usersRead;
        private readonly IUserUpdateOnlyRepository _usersUpdate;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITransactionsReadOnlyRepository _transactions;
        private readonly IDashboardReadOnlyRepository _dashboard;
        private readonly ISavingsGoalReadOnlyRepository _savingsGoals;
        private readonly ITransactionalEmailService _emails;
        private readonly IPushNotificationService _push;
        private readonly ILogger<MonthlySummaryJob> _logger;

        public MonthlySummaryJob (
            IUserReadOnlyRepository usersRead ,
            IUserUpdateOnlyRepository usersUpdate ,
            IUnitOfWork unitOfWork ,
            ITransactionsReadOnlyRepository transactions ,
            IDashboardReadOnlyRepository dashboard ,
            ISavingsGoalReadOnlyRepository savingsGoals ,
            ITransactionalEmailService emails ,
            IPushNotificationService push ,
            ILogger<MonthlySummaryJob> logger )
        {
            _usersRead = usersRead;
            _usersUpdate = usersUpdate;
            _unitOfWork = unitOfWork;
            _transactions = transactions;
            _dashboard = dashboard;
            _savingsGoals = savingsGoals;
            _emails = emails;
            _push = push;
            _logger = logger;
        }

        public async Task ExecuteAsync ( long userId , int year , int month , CancellationToken cancellationToken )
        {
            var period = new DateOnly(year , month , 1);

            var found = await _usersRead.GetByIdAsync(userId , cancellationToken);
            if ( found is null || found.EmailVerifiedAt is null ) {
                return;
            }
            if ( found.MonthlySummarySentFor >= period || ( found.MonthlySummaryEmail == false && found.MonthlySummaryPush == false ) ) {
                return;
            }

            // DbContext is not thread-safe: queries run sequentially.
            var expenses = await _transactions.GetExpensesOfMonthAsync(userId , year , month , cancellationToken);
            var incomes = await _transactions.GetIncomesOfMonthAsync(userId , year , month , cancellationToken);

            if ( expenses.Count > 0 || incomes.Count > 0 ) {
                var previous = period.AddMonths(-1);
                var previousIncome = await _dashboard.GetTotalAmountIncomesByMonth(userId , previous.Year , previous.Month , cancellationToken);
                var previousExpenses = await _dashboard.GetTotalAmountExpensesByMonth(userId , previous.Year , previous.Month , cancellationToken);
                var goals = await _savingsGoals.GetAllWithBalanceAsync(userId , cancellationToken);

                var totalIncome = incomes.Sum(i => i.Amount);
                var totalExpenses = expenses.Sum(e => e.Amount);
                var largest = expenses.OrderByDescending(e => e.Amount).FirstOrDefault();
                var focus = goals
                    .Where(g => g.Goal.TargetAmount > 0 && g.SavedAmount < g.Goal.TargetAmount)
                    .OrderByDescending(g => g.SavedAmount / g.Goal.TargetAmount)
                    .FirstOrDefault();

                var email = new MonthlySummaryEmail(
                    found.Name ,
                    year ,
                    month ,
                    totalIncome ,
                    totalExpenses ,
                    previousIncome ,
                    previousExpenses ,
                    expenses
                        .GroupBy(e => e.Category?.Name ?? "Sem categoria")
                        .Select(g => new CategorySpending(g.Key , g.Sum(e => e.Amount)))
                        .ToList() ,
                    expenses.Count + incomes.Count ,
                    largest is null ? null : new LargestExpense(largest.Title , largest.Amount) ,
                    focus is null ? null : new GoalProgress(focus.Goal.Name , (int) Math.Floor(focus.SavedAmount / focus.Goal.TargetAmount * 100)));

                if ( found.MonthlySummaryEmail ) {
                    // The idempotency key also protects a Hangfire retry right after a send that did go out.
                    await EmailJobSender.SendAsync(
                        () => _emails.SendAsync(found.Email , email , $"monthly-summary-{userId}-{period:yyyy-MM}" , cancellationToken) ,
                        _logger , "monthly-summary" , userId);
                }

                if ( found.MonthlySummaryPush ) {
                    await SendPushAsync(userId , period , totalIncome , totalExpenses , cancellationToken);
                }
            }

            var user = await _usersUpdate.GetById(userId);
            user.MonthlySummarySentFor = period;
            _usersUpdate.Update(user);
            await _unitOfWork.Commit(cancellationToken);
        }

        private async Task SendPushAsync ( long userId , DateOnly period , decimal income , decimal expenses , CancellationToken cancellationToken )
        {
            var monthName = period.ToString("MMMM" , PtBr);
            try {
                await _push.EnqueueAsync(userId , new PushMessage(
                    PUSH_TYPE ,
                    $"Seu resumo de {monthName} chegou" ,
                    $"Receitas {income.ToString("C2" , PtBr)} e despesas {expenses.ToString("C2" , PtBr)}. Veja para onde foi seu dinheiro." ,
                    ROUTE ,
                    $"{PUSH_TYPE}:{userId}:{period:yyyy-MM}") , cancellationToken);
            } catch ( System.Exception ex ) when ( ex is not OperationCanceledException ) {
                // The e-mail (when chosen) already went out; a push failure must not resend it on retry.
                _logger.LogWarning(ex , "Push do resumo mensal não enviado ao usuário {UserId}." , userId);
            }
        }
    }

    public static class MonthlySummary
    {
        /// <summary>First day of the month before <paramref name="today"/> (the month that just ended).</summary>
        public static DateOnly PreviousMonth ( DateOnly today ) => new DateOnly(today.Year , today.Month , 1).AddMonths(-1);
    }
}
