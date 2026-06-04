using AutoMapper;
using MiCash.Communication.Responses.Categories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Categories.GetById
{
    public class GetCategoryByIdUseCase : IGetCategoryByIdUseCase
    {
        private readonly ICategoriesReadOnlyRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;

        public GetCategoryByIdUseCase (
            ICategoriesReadOnlyRepository repository,
            IMapper mapper,
            ILoggedUser loggedUser)
        {
            _repository = repository;
            _mapper = mapper;
            _loggedUser = loggedUser;
        }
        public async Task<IEnumerable<ResponseCategoryJson>> Execute ( long id, CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var result = await _repository.GetByIdAsync(loggedUser , id, cancellationToken);

            if( result == null ) 
            {
                throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);
            }

            return _mapper.Map<IEnumerable<ResponseCategoryJson>>(result);
        }
    }
}
