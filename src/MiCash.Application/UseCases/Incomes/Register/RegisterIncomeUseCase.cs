using AutoMapper;
using MiCash.Communication.Requests.Incomes;
using MiCash.Communication.Responses.Incomes;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Incomes.Register
{
    public class RegisterIncomeUseCase : IRegisterIncomeUseCase
    {
        private readonly IIncomesWriteOnlyRepository _incomesWriteOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggedUser _loggedUser;
        private readonly IMapper _mapper;
        private readonly IRedisCacheService _redisCacheService;

        public RegisterIncomeUseCase (
            IIncomesWriteOnlyRepository incomesWriteOnlyRepository,
            IUnitOfWork unitOfWork,
            ILoggedUser loggedUser,
            IMapper mapper,
            IRedisCacheService redisCacheService)
        {
            _incomesWriteOnlyRepository = incomesWriteOnlyRepository;
            _unitOfWork = unitOfWork;
            _loggedUser = loggedUser;
            _mapper = mapper;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponseRegisteredIncomeJson> Execute ( RequestIncomeJson request , CancellationToken cancellationToken )
        {
            Validate(request);

            var loggedUser = await _loggedUser.Get();

            var income = _mapper.Map<Income>(request);
            income.Category = ( Domain.Enums.IncomeCategory )  request.IncomeCategory ;
            income.UserId = loggedUser.Id;

            await _incomesWriteOnlyRepository.Add(income , cancellationToken);
            await _unitOfWork.Commit(cancellationToken);

            var cacheKey =
                $"dashboard:monthly-summary:{loggedUser.Id}:{income.ReceivedAt.Year}:{income.ReceivedAt.Month}";

            await _redisCacheService.RemoveAsync(cacheKey);

            await _redisCacheService.RemoveAsync(
                CacheKeys.ExpensesByCategory(
                    loggedUser.Id ,
                    income.ReceivedAt.Year ,
                    income.ReceivedAt.Month));

            return _mapper.Map<ResponseRegisteredIncomeJson>(income);
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
