using AutoMapper;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Expenses.GetAll
{
    public class GetAllExpensesUseCase : IGetAllExpensesUseCase
    {
        private readonly IExpensesReadOnlyRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;
        public GetAllExpensesUseCase (IExpensesReadOnlyRepository repository, IMapper mapper, ILoggedUser loggedUser)
        {
            _repository = repository;
            _mapper = mapper;
            _loggedUser = loggedUser;

        }
        public async Task<ResponseExpensesJson> Execute ( CancellationToken cancellationToken)
        {
            var userLogged = await _loggedUser.Get();

            var result = await _repository.GetAll(userLogged, cancellationToken);

            return new ResponseExpensesJson
            {
                Expenses = _mapper.Map<List<ResponseShortExpenseJson>>(result)
            };
        }
    }
}
