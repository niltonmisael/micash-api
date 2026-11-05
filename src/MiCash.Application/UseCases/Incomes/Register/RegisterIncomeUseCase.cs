using MiCash.Application.Mappers;
using MiCash.Communication.Requests.Incomes;
using MiCash.Communication.Responses.Incomes;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Incomes.Register
{
    public class RegisterIncomeUseCase : IRegisterIncomeUseCase
    {
        private readonly IIncomesWriteOnlyRepository _incomesWriteOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public RegisterIncomeUseCase (
            IIncomesWriteOnlyRepository incomesWriteOnlyRepository,
            IUnitOfWork unitOfWork,
            ILoggedUser loggedUser,
            IFinancialCacheInvalidator financialCacheInvalidator)
        {
            _incomesWriteOnlyRepository = incomesWriteOnlyRepository;
            _unitOfWork = unitOfWork;
            _loggedUser = loggedUser;
            _financialCacheInvalidator = financialCacheInvalidator;
        }
        public async Task<ResponseRegisteredIncomeJson> Execute ( RequestIncomeJson request , CancellationToken cancellationToken )
        {
            Validate(request);

            var loggedUser = await _loggedUser.Get();

            var income = request.ToEntity();
            income.UserId = loggedUser.Id;

            await _incomesWriteOnlyRepository.Add(income , cancellationToken);
            await _unitOfWork.Commit(cancellationToken);

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                loggedUser.Id ,
                income.ReceivedAt.Month ,
                income.ReceivedAt.Year);

            return income.ToRegisteredResponse();
        }

        private void Validate ( RequestIncomeJson request )
        {
            var validator = new IncomeValidator();

            var result = validator.Validate(request);

            if ( result.IsValid == false ) {

                var errorMessage = result.Errors.Select(f => f.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessage);

            }
        }
    }
}
