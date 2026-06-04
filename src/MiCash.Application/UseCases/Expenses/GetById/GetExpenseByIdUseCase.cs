using AutoMapper;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Expenses;
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

        public GetExpenseByIdUseCase (IExpensesReadOnlyRepository repository, IMapper mapper, ILoggedUser loggedUser)
        {
            _repository = repository;
            _mapper = mapper;
            _loggedUser = loggedUser;
        }
        public async Task<ResponseExpenseJson> Execute (long id , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var result = await _repository.GetById (loggedUser, id, cancellationToken );

            if ( result is null ) {

                throw new NotFoundException(ResourceErrorMessages.EXPENSE_NOT_FOUND);
            }

            return _mapper.Map<ResponseExpenseJson>(result);
        }
    }
}
