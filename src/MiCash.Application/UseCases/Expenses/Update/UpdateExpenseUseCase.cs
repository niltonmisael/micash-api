using AutoMapper;
using MiCash.Communication.Requests;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Expenses.Update
{
    public class UpdateExpenseUseCase : IUpdateExpenseUseCase
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExpensesUpdateOnlyRepository _repository;

        public UpdateExpenseUseCase (IMapper mapper, IUnitOfWork unitOfWork, IExpensesUpdateOnlyRepository repository)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _repository = repository;
        }
        public async Task Execute (long id, RequestExpenseJson request)
        {
            Validate(request);
            var expense = await _repository.GetById(id);

            if ( expense == null ) {
                throw new NotFoundException(ResourseErrorMessages.EXPENSE_NOT_FOUND);   
            }

            _mapper.Map(request, expense);

            _repository.Update(expense);

            await _unitOfWork.Commit();
        }

        public void Validate ( RequestExpenseJson request ) {

            var validator = new ExpensesValidator();

            var result = validator.Validate ( request );
            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(e => e.ErrorMessage ).ToList();
                throw new ErrorOnValidationException( errorMessages );
            }
        }
    }
}
