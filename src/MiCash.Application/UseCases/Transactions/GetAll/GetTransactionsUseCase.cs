using MiCash.Communication.Enums;
using MiCash.Communication.Pagination;
using MiCash.Communication.Requests.Transactions;
using MiCash.Communication.Responses.Transactions;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Transactions;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using System.Globalization;

namespace MiCash.Application.UseCases.Transactions.GetAll
{
    public interface IGetTransactionsUseCase
    {
        Task<ResponsePagedJson<ResponseTransactionJson>> Execute ( RequestTransactionsJson request , CancellationToken cancellationToken );
    }

    /// <summary>
    /// Incomes and expenses of one month in a single list (web "Transações" screen), newest first, filtered and
    /// paginated here. A month of one user is small, so both kinds are loaded and merged in memory.
    /// Not limited by plan: entry lists are never hidden (only dashboard/goals/insights history is).
    /// </summary>
    public sealed class GetTransactionsUseCase : IGetTransactionsUseCase
    {
        public const int MAX_PAGE_SIZE = 100;

        private static readonly CompareInfo PtBr = CultureInfo.GetCultureInfo("pt-BR").CompareInfo;

        private readonly ILoggedUser _loggedUser;
        private readonly ITransactionsReadOnlyRepository _repository;

        public GetTransactionsUseCase ( ILoggedUser loggedUser , ITransactionsReadOnlyRepository repository )
        {
            _loggedUser = loggedUser;
            _repository = repository;
        }

        public async Task<ResponsePagedJson<ResponseTransactionJson>> Execute ( RequestTransactionsJson request , CancellationToken cancellationToken )
        {
            Validate(request);

            var user = await _loggedUser.Get();

            // A category filter of one kind excludes the other kind.
            var wantsExpenses = request.Type != TransactionType.Income && request.IncomeCategory is null;
            var wantsIncomes = request.Type != TransactionType.Expense && request.CategoryId is null;

            // DbContext is not thread-safe: queries run sequentially.
            IReadOnlyList<Expense> expenses = wantsExpenses
                ? await _repository.GetExpensesOfMonthAsync(user.Id , request.Year , request.Month , cancellationToken)
                : [];
            IReadOnlyList<Income> incomes = wantsIncomes
                ? await _repository.GetIncomesOfMonthAsync(user.Id , request.Year , request.Month , cancellationToken)
                : [];

            return Build(request , expenses , incomes);
        }

        public static ResponsePagedJson<ResponseTransactionJson> Build (
            RequestTransactionsJson request ,
            IEnumerable<Expense> expenses ,
            IEnumerable<Income> incomes )
        {
            var search = request.Search?.Trim();

            var items = expenses
                .Where(expense => request.CategoryId is null || expense.CategoryId == request.CategoryId)
                .Select(expense => (Item: FromExpense(expense), expense.CreatedAt))
                .Concat(incomes
                    .Where(income => request.IncomeCategory is null || (int) income.Category == (int) request.IncomeCategory)
                    .Select(income => (Item: FromIncome(income), income.CreatedAt)))
                .Where(entry => string.IsNullOrEmpty(search) || ContainsIgnoringCaseAndAccents(entry.Item.Title , search))
                // Newest day first; inside a day, the expense time (or the creation time) breaks the tie.
                .OrderByDescending(entry => entry.Item.Date)
                .ThenByDescending(entry => entry.Item.ExpenseDate ?? entry.CreatedAt)
                .ThenByDescending(entry => entry.Item.Id)
                .Select(entry => entry.Item)
                .ToList();

            var page = Math.Max(1 , request.Page);
            var pageSize = Math.Clamp(request.PageSize , 1 , MAX_PAGE_SIZE);

            return new ResponsePagedJson<ResponseTransactionJson>
            {
                Data = items.Skip((page - 1) * pageSize).Take(pageSize).ToList() ,
                Page = page ,
                PageSize = pageSize ,
                TotalRecords = items.Count
            };
        }

        private static void Validate ( RequestTransactionsJson request )
        {
            PeriodValidator.Validate(request.Month , request.Year);

            var errors = new List<string>();

            if ( request.Type is not null && Enum.IsDefined(request.Type.Value) == false ) {
                errors.Add(ResourceErrorMessages.TRANSACTION_TYPE_INVALID);
            }

            if ( request.IncomeCategory is not null && Enum.IsDefined(request.IncomeCategory.Value) == false ) {
                errors.Add(ResourceErrorMessages.INCOME_TYPE_IS_INVALID);
            }

            if ( errors.Count > 0 ) {
                throw new ErrorOnValidationException(errors);
            }
        }

        private static ResponseTransactionJson FromExpense ( Expense expense ) => new()
        {
            Id = expense.Id ,
            Type = TransactionType.Expense ,
            Title = expense.Title ,
            Description = expense.Description ,
            Amount = expense.Amount ,
            Date = DateOnly.FromDateTime(BrazilClock.ToBrasilia(expense.ExpenseDate)) ,
            ExpenseDate = expense.ExpenseDate ,
            CategoryId = expense.CategoryId ,
            CategoryName = expense.Category?.Name ,
            CategoryColor = expense.Category?.Color ,
            CategoryIcon = expense.Category?.Icon ,
            PaymentType = (PaymentType) (int) expense.PaymentType ,
            ExpenseStatus = (ExpenseStatus) (int) expense.Status
        };

        private static ResponseTransactionJson FromIncome ( Income income ) => new()
        {
            Id = income.Id ,
            Type = TransactionType.Income ,
            Title = income.Title ,
            Description = income.Description ,
            Amount = income.Amount ,
            Date = income.ReceivedAt ,
            IncomeCategory = (IncomeCategory) (int) income.Category ,
            IsRecurring = income.IsRecurring
        };

        private static bool ContainsIgnoringCaseAndAccents ( string text , string search ) =>
            PtBr.IndexOf(text , search , CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }
}
