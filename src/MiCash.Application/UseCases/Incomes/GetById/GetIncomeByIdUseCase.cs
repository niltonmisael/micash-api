using AutoMapper;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Incomes;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Incomes.GetById
{
    public class GetIncomeByIdUseCase : IGetIncomeByIdUseCase
    {
        private readonly IIncomesReadOnlyRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;

        public GetIncomeByIdUseCase (
            IIncomesReadOnlyRepository repository,
            IMapper mapper,
            ILoggedUser loggedUser)
        {
            _repository = repository;
            _mapper = mapper;
            _loggedUser = loggedUser;
        }
        public async Task<ResponseIncomeJson> Execute ( long id , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var result = await _repository.GetByIdAsync(loggedUser.Id , id, cancellationToken);

            if ( result is null ) {

                throw new NotFoundException(ResourceErrorMessages.EXPENSE_NOT_FOUND);
            }

            return _mapper.Map<ResponseIncomeJson>(result);
        }
    }
}
