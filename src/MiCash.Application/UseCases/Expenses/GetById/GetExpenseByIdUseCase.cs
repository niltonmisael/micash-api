using AutoMapper;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Expenses.GetById
{
    public class GetExpenseByIdUseCase : IGetExpenseByIdUseCase
    {
        private readonly IExpensesReadOnlyRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;
        private readonly IRedisCacheService _redisCacheService;

        public GetExpenseByIdUseCase (
            IExpensesReadOnlyRepository repository, 
            IMapper mapper, 
            ILoggedUser loggedUser,
            IRedisCacheService redisCacheService)
        {
            _repository = repository;
            _mapper = mapper;
            _loggedUser = loggedUser;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponseExpenseJson> Execute (long id , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var cacheKey = CacheKeys.ExpenseById(loggedUser.Id, id);

            var cached = await _redisCacheService.GetAsync<ResponseExpenseJson>(cacheKey);

            if (cached != null) {
                return cached;
            }

            var result = await _repository.GetById (loggedUser, id, cancellationToken );

            if ( result is null ) {

                throw new NotFoundException(ResourceErrorMessages.EXPENSE_NOT_FOUND);
            }

            var response = _mapper.Map<ResponseExpenseJson>(result);

            await _redisCacheService.SetAsync(
                cacheKey, 
                response,
                CacheExpiration.Expense);

            return response;
        }
    }
}
