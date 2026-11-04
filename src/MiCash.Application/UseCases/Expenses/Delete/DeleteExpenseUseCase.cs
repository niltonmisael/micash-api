using MiCash.Domain.Entities;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Expenses.Delete
{
    public class DeleteExpenseUseCase : IDeleteExpenseUseCase
    {
        private readonly IExpensesReadOnlyRepository _expensesReadOnlyRepository;
        private readonly IExpensesWriteOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;
        private readonly IRedisCacheService _redisCacheService;
        public DeleteExpenseUseCase (
            IExpensesWriteOnlyRepository repository, 
            IUnitOfWork unitOfWork, 
            ILoggedUser loggedUser, 
            IExpensesReadOnlyRepository expensesReadOnlyRepository,
            IFinancialCacheInvalidator financialCacheInvalidator,
            IRedisCacheService redisCacheService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _loggedUser = loggedUser;
            _expensesReadOnlyRepository = expensesReadOnlyRepository;
            _financialCacheInvalidator = financialCacheInvalidator;
            _redisCacheService = redisCacheService;
        }
        public async Task Execute(long id, CancellationToken cancellationToken)
        {
            var loggedUser = await _loggedUser.Get();

            var expense = await _expensesReadOnlyRepository.GetById(loggedUser,id, cancellationToken);

            if ( expense == null ) {
                throw new NotFoundException(ResourceErrorMessages.EXPENSE_NOT_FOUND);
            }

            await _repository.Delete(id, cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            await _redisCacheService.RemoveAsync(
                CacheKeys.ExpenseById(
                    loggedUser.Id ,
                    expense.Id));


            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                loggedUser.Id ,
                BrazilClock.ToBrasilia(expense.ExpenseDate).Month ,
                BrazilClock.ToBrasilia(expense.ExpenseDate).Year);

        }
    }
}
