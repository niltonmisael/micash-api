using AutoMapper;
using MiCash.Application.Services.FinancialGoals;
using MiCash.Communication.Requests;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Expenses.Update
{
    public class UpdateExpenseUseCase : IUpdateExpenseUseCase
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExpensesUpdateOnlyRepository _repository;
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator ;
        private readonly IRedisCacheService _redisCacheService ;
        private readonly IFinancialGoalAlertNotifier _goalAlertNotifier;

        public UpdateExpenseUseCase (
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IExpensesUpdateOnlyRepository repository,
            ILoggedUser loggedUser,
            IFinancialCacheInvalidator financialCacheInvalidator,
            IRedisCacheService redisCacheService,
            IFinancialGoalAlertNotifier goalAlertNotifier)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _repository = repository;
            _loggedUser = loggedUser;
            _financialCacheInvalidator = financialCacheInvalidator;
            _redisCacheService = redisCacheService;
            _goalAlertNotifier = goalAlertNotifier;
        }
        public async Task Execute (long id, RequestExpenseJson request, CancellationToken cancellationToken)
        {
            Validate(request);

            var user = await _loggedUser.Get();

            var expense = await _repository.GetById(
                user ,
                id ,
                cancellationToken);

            if ( expense is null ) {
                throw new NotFoundException(
                    ResourceErrorMessages.EXPENSE_NOT_FOUND);
            }

            // Months of the Brasília calendar (ExpenseDate is a UTC instant), same as the dashboard queries.
            var oldExpenseDate = BrazilClock.ToBrasilia(expense.ExpenseDate);

            _mapper.Map(request , expense);

            _repository.Update(expense , cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            await _redisCacheService.RemoveAsync(
                CacheKeys.ExpenseById(
                    user.Id ,
                    expense.Id));

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                    user.Id ,
                    oldExpenseDate.Month ,
                    oldExpenseDate.Year);

            var newExpenseDate = BrazilClock.ToBrasilia(expense.ExpenseDate);

            if ( oldExpenseDate.Month != newExpenseDate.Month ||
                oldExpenseDate.Year != newExpenseDate.Year ) {

                await _financialCacheInvalidator
                    .InvalidateMonthAsync(
                        user.Id ,
                        newExpenseDate.Month ,
                        newExpenseDate.Year);
            }

            // A higher amount, another category or a move to this month can cross a goal threshold.
            await _goalAlertNotifier.NotifyAsync(user , expense.CategoryId , expense.ExpenseDate , cancellationToken);

        }

        public void Validate ( RequestExpenseJson request ) {

            var validator = new ExpenseValidator();

            var result = validator.Validate ( request );
            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(e => e.ErrorMessage ).ToList();
                throw new ErrorOnValidationException( errorMessages );
            }
        }
    }
}
