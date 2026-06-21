using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Incomes.Delete
{
    public class DeleteIncomeUseCase : IDeleteIncomeUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIncomesWriteOnlyRepository _incomesWriteOnlyRepository;
        private readonly IIncomesReadOnlyRepository _incomesReadOnlyRepository;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public DeleteIncomeUseCase (
            ILoggedUser loggedUser,
            IUnitOfWork unitOfWork,
            IIncomesWriteOnlyRepository incomesWriteOnlyRepository,
            IIncomesReadOnlyRepository incomesReadOnlyRepository,
            IFinancialCacheInvalidator financialCacheInvalidator)
        {
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _incomesWriteOnlyRepository = incomesWriteOnlyRepository;
            _incomesReadOnlyRepository = incomesReadOnlyRepository;
            _financialCacheInvalidator = financialCacheInvalidator;
        }
        public async Task Execute ( long id , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var income = await _incomesReadOnlyRepository.GetByIdAsync(loggedUser.Id , id , cancellationToken);

            if( income is null ) {
                throw new NotFoundException(ResourceErrorMessages.INCOME_NOT_FOUND);
            }

            await _incomesWriteOnlyRepository.DeleteAsync(income.Id , cancellationToken);
            await _unitOfWork.Commit(cancellationToken);

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                loggedUser.Id ,
                income.ReceivedAt.Month ,
                income.ReceivedAt.Year);
        }

    }
}
