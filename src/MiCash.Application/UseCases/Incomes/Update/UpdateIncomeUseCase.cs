using MiCash.Application.Mappers;
using MiCash.Communication.Requests.Incomes;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Incomes.Update
{
    public class UpdateIncomeUseCase : IUpdateIncomeUseCase
    {
        private readonly IIncomesUpdateOnlyRepository _incomesUpdateOnlyRepository;
        private readonly IIncomesReadOnlyRepository _incomesReadOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;
        private readonly IRedisCacheService _redisCacheService;

        public UpdateIncomeUseCase (
            IIncomesUpdateOnlyRepository incomesUpdateOnlyRepository ,
            IIncomesReadOnlyRepository incomesReadOnlyRepository ,
            IUnitOfWork unitOfWork ,
            ILoggedUser loggedUser ,
            IFinancialCacheInvalidator financialCacheInvalidator ,
            IRedisCacheService redisCacheService )
        {
            _incomesUpdateOnlyRepository = incomesUpdateOnlyRepository;
            _incomesReadOnlyRepository = incomesReadOnlyRepository;
            _unitOfWork = unitOfWork;
            _loggedUser = loggedUser;
            _financialCacheInvalidator = financialCacheInvalidator;
            _redisCacheService = redisCacheService;
        }
        public async Task Execute (
            long id ,
            RequestIncomeJson request ,
            CancellationToken cancellationToken )
        {
            Validate(request);

            var user = await _loggedUser.Get();

            var income = await _incomesReadOnlyRepository
                .GetByIdAsync(
                    user.Id ,
                    id ,
                    cancellationToken);

            if ( income is null ) {
                throw new NotFoundException(
                    ResourceErrorMessages.INCOME_NOT_FOUND);
            }

            var oldReceivedAt = income.ReceivedAt;

            request.ApplyTo(income);

            await _incomesUpdateOnlyRepository
                .UpdateAsync(
                    income ,
                    cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            await _redisCacheService.RemoveAsync(
                CacheKeys.IncomeById(
                    user.Id ,
                    income.Id));

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                    user.Id ,
                    oldReceivedAt.Month ,
                    oldReceivedAt.Year);

            if ( oldReceivedAt.Month != income.ReceivedAt.Month ||
                oldReceivedAt.Year != income.ReceivedAt.Year ) {
                await _financialCacheInvalidator
                    .InvalidateMonthAsync(
                        user.Id ,
                        income.ReceivedAt.Month ,
                        income.ReceivedAt.Year);
            }
        }

        private void Validate ( RequestIncomeJson request )
        {
            var validator = new IncomeValidator();
            var result = validator.Validate(request);
            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(e => e.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errorMessages);
            }
        }
    }
}
