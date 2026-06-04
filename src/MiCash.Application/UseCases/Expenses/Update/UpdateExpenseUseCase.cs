using AutoMapper;
using MiCash.Communication.Requests;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Cache;
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
        private readonly IRedisCacheService _cacheService;

        public UpdateExpenseUseCase (
            IMapper mapper, 
            IUnitOfWork unitOfWork, 
            IExpensesUpdateOnlyRepository repository, 
            ILoggedUser loggedUser,
            IRedisCacheService cacheService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _repository = repository;
            _loggedUser = loggedUser;
            _cacheService = cacheService;
        }
        public async Task Execute (long id, RequestExpenseJson request, CancellationToken cancellationToken)
        {
            Validate(request);

            var loggedUser = await _loggedUser.Get();

            var expense = await _repository.GetById(loggedUser, id, cancellationToken);

            if ( expense == null  ) {
                throw new NotFoundException(ResourceErrorMessages.EXPENSE_NOT_FOUND);   
            }

            _mapper.Map(request, expense);

            _repository.Update(expense, cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            var cacheKey =
                $"dashboard:monthly-summary:{loggedUser.Id}:{expense.ExpenseDate.Year}:{expense.ExpenseDate.Month}";

            await _cacheService.RemoveAsync(cacheKey);

            await _cacheService.RemoveAsync(
                CacheKeys.ExpensesByCategory(
                    loggedUser.Id ,
                    expense.ExpenseDate.Year ,
                    expense.ExpenseDate.Month));
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
