using AutoMapper;
using MiCash.Communication.Requests;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
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
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator ;

        public UpdateExpenseUseCase (
            IMapper mapper, 
            IUnitOfWork unitOfWork, 
            IExpensesUpdateOnlyRepository repository, 
            ILoggedUser loggedUser,
            IFinancialCacheInvalidator financialCacheInvalidator)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _repository = repository;
            _loggedUser = loggedUser;
            _financialCacheInvalidator = financialCacheInvalidator;
        }
        public async Task Execute (long id, RequestExpenseJson request, CancellationToken cancellationToken)
        {
            Validate(request);

            var user = await _loggedUser.Get();

            var expense = await _repository.GetById(
                user ,
                id ,
                cancellationToken);

            if ( expense is null ) {
                throw new NotFoundException(
                    ResourceErrorMessages.EXPENSE_NOT_FOUND);
            }

            var oldExpenseDate = expense.ExpenseDate;

            _mapper.Map(request , expense);

            _repository.Update(expense , cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                    user.Id ,
                    oldExpenseDate.Month ,
                    oldExpenseDate.Year);

            if ( oldExpenseDate.Month != expense.ExpenseDate.Month ||
                oldExpenseDate.Year != expense.ExpenseDate.Year ) {

                await _financialCacheInvalidator
                    .InvalidateMonthAsync(
                        user.Id ,
                        expense.ExpenseDate.Month ,
                        expense.ExpenseDate.Year);
            }

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
