using MiCash.Application.UseCases.FinancialGols.Copy;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGoals.Copy
{
    public class CopyFinancialGoalsUseCase : ICopyFinancialGoalsUseCase
    {
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly IFinancialGoalWriteOnlyRepository _financialGoalWriteOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public CopyFinancialGoalsUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            IFinancialGoalWriteOnlyRepository financialGoalWriteOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            IFinancialCacheInvalidator financialCacheInvalidator )
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _financialGoalWriteOnlyRepository = financialGoalWriteOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _financialCacheInvalidator = financialCacheInvalidator;
        }

        public async Task Execute (
            RequestCopyFinancialGoalsJson request ,
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var sourceGoals =
                await _financialGoalReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        request.SourceMonth ,
                        request.SourceYear ,
                        cancellationToken);

            if ( !sourceGoals.Any() ) {
                throw new ErrorOnValidationException(
                    new List<string>
                    {
                        "Não existem metas cadastradas no período de origem."
                    });
            }

            var targetGoals =
                await _financialGoalReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        request.TargetMonth ,
                        request.TargetYear ,
                        cancellationToken);

            if ( targetGoals.Any() ) {
                throw new ErrorOnValidationException(
                    new List<string>
                    {
                        "O período de destino já possui metas cadastradas."
                    });
            }

            var financialGoals = sourceGoals
                .Select(goal => new FinancialGoal
                {
                    UserId = user.Id ,
                    CategoryId = goal.CategoryId ,
                    TargetAmount = goal.TargetAmount ,
                    Month = request.TargetMonth ,
                    Year = request.TargetYear ,
                    CreatedAt = DateTime.UtcNow ,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            await _financialGoalWriteOnlyRepository
                .AddRangeAsync(
                    financialGoals ,
                    cancellationToken);

            await _financialGoalWriteOnlyRepository
                .AddRangeAsync(
                financialGoals ,
                cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                    user.Id ,
                    request.TargetMonth ,
                    request.TargetYear);

        }
    }
}