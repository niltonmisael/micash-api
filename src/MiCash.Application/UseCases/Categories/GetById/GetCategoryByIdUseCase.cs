using MiCash.Application.Mappers;
using MiCash.Communication.Responses.Categories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Categories.GetById
{
    public class GetCategoryByIdUseCase : IGetCategoryByIdUseCase
    {
        private readonly ICategoriesReadOnlyRepository _repository;
        private readonly ILoggedUser _loggedUser;
        private readonly IRedisCacheService _redisCacheService;

        public GetCategoryByIdUseCase (
            ICategoriesReadOnlyRepository repository,
            ILoggedUser loggedUser,
            IRedisCacheService redisCacheService)
        {
            _repository = repository;
            _loggedUser = loggedUser;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponseCategoryJson> Execute ( long id, CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var cacheKey = CacheKeys.CategoryById(loggedUser.Id, id);

            var cached = await _redisCacheService.GetAsync<ResponseCategoryJson>(cacheKey);

            if( cached != null ) {
                return cached;
            }

            var result = await _repository.GetByIdAsync(loggedUser , id, cancellationToken);

            if( result == null ) 
            {
                throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);
            }

            var response = result.ToResponse();

            await _redisCacheService.SetAsync(
                cacheKey, 
                response,
                CacheExpiration.Category);

            return response;
        }
    }
}
