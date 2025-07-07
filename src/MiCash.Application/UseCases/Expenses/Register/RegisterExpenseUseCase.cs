using AutoMapper;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Expenses.Register
{
    public class RegisterExpenseUseCase : IRegisterExpenseUseCase
    {
        private readonly IExpensesWriteOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RegisterExpenseUseCase ( IExpensesWriteOnlyRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ResponseRegisteredExpenseJson> Execute (RequestExpenseJson request) 
        {
            Validate(request);

            var entity = _mapper.Map<Expense>(request);

             await _repository.Add(entity);
             await _unitOfWork.Commit();

            return _mapper.Map<ResponseRegisteredExpenseJson>(entity);

        }

        private void Validate ( RequestExpenseJson request )
        {
            var validator = new ExpensesValidator();

            var result = validator.Validate(request);

            if ( result.IsValid == false ) {

                var errorMessage = result.Errors.Select(f => f.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessage);

            }
        }
    }
}
