using AutoMapper;
using MiCash.Communication.Requests.Incomes;
using MiCash.Communication.Responses.Incomes;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Incomes.GetAll
{
    public class GetAllIncomesUseCase : IGetAllIncomesUseCase
    {
        private readonly IIncomesReadOnlyRepository _repository;
        private readonly ILoggedUser _loggedUser;
        private readonly IMapper _mapper;

        public GetAllIncomesUseCase (
            IIncomesReadOnlyRepository repository,
            ILoggedUser loggedUser,
            IMapper mapper)
        {
            _repository = repository;
            _loggedUser = loggedUser;
            _mapper = mapper;
        }
        public async Task<ResponseIncomesJson> Execute ( CancellationToken cancellationToken )
        {
            var userLogged = await _loggedUser.Get();

            var result = await _repository.GetAllAsync(userLogged, cancellationToken);

            return new ResponseIncomesJson
            {
                Incomes = _mapper.Map<List<ResponseIncomeShortJson>>(result)
            };
        }
    }
}
