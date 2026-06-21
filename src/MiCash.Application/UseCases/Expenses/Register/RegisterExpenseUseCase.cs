using AutoMapper;
using DocumentFormat.OpenXml.Spreadsheet;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Expenses.Register
{
    public class RegisterExpenseUseCase : IRegisterExpenseUseCase
    {
        private readonly IExpensesWriteOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;
        private readonly IRedisCacheService _cacheService;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public RegisterExpenseUseCase ( 
            IExpensesWriteOnlyRepository repository, 
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            ILoggedUser loggedUser,
            IRedisCacheService cacheService,
            IFinancialCacheInvalidator financialCacheInvalidator)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _loggedUser = loggedUser;
            _cacheService = cacheService;
            _financialCacheInvalidator = financialCacheInvalidator;
        }

        public async Task<ResponseRegisteredExpenseJson> Execute (RequestExpenseJson request, CancellationToken cancellationToken) 
        {
            Validate(request);

            var loggedUser = await _loggedUser.Get();

            var expense = _mapper.Map<Expense>(request);
            expense.UserId = loggedUser.Id;
            expense.Status = ( Domain.Enums.ExpenseStatus )  request.ExpenseStatus ;

            await _repository.Add(expense, cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                loggedUser.Id ,
                expense.ExpenseDate.Month ,
                expense.ExpenseDate.Year);

            return _mapper.Map<ResponseRegisteredExpenseJson>(expense);

        }

        private void Validate ( RequestExpenseJson request )
        {
            var validator = new ExpenseValidator();

            var result = validator.Validate(request);

            if ( result.IsValid == false ) {

                var errorMessage = result.Errors.Select(f => f.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessage);

            }
        }
    }
}
