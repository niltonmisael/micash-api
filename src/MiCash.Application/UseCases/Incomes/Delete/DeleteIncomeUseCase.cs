using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Cache;
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
        private readonly IRedisCacheService _cacheService;

        public DeleteIncomeUseCase (
            ILoggedUser loggedUser,
            IUnitOfWork unitOfWork,
            IIncomesWriteOnlyRepository incomesWriteOnlyRepository,
            IIncomesReadOnlyRepository incomesReadOnlyRepository,
            IRedisCacheService cacheService)
        {
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _incomesWriteOnlyRepository = incomesWriteOnlyRepository;
            _incomesReadOnlyRepository = incomesReadOnlyRepository;
            _cacheService = cacheService;
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

            var cacheKey =
                $"dashboard:monthly-summary:{loggedUser.Id}:{income.ReceivedAt.Year}:{income.ReceivedAt.Month}";

            await _cacheService.RemoveAsync(cacheKey);

            await _cacheService.RemoveAsync(
                CacheKeys.ExpensesByCategory(
                    loggedUser.Id ,
                    income.ReceivedAt.Year ,
                    income.ReceivedAt.Month));
        }

    }
}
