using AutoMapper;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
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

        public RegisterExpenseUseCase ( 
            IExpensesWriteOnlyRepository repository, 
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            ILoggedUser loggedUser)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _loggedUser = loggedUser;
        }

        public async Task<ResponseRegisteredExpenseJson> Execute (RequestExpenseJson request, CancellationToken cancellationToken) 
        {
            Validate(request);

            var loggedUser = await _loggedUser.Get();

            var expense = _mapper.Map<Expense>(request);
            expense.UserId = loggedUser.Id;

            await _repository.Add(expense, cancellationToken);
            await _unitOfWork.Commit(cancellationToken);

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
