using MiCash.Application.Mappers;
using MiCash.Communication.Responses.Categories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Categories.GetAll
{
    public class GetAllCategoryUseCase : IGetAllCategoryUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly ICategoriesReadOnlyRepository _repostory;
        private readonly IRedisCacheService _redisCacheService;

        public GetAllCategoryUseCase (
            ILoggedUser loggedUser,
            ICategoriesReadOnlyRepository repostory,
            IRedisCacheService redisCacheService)
        {
            _loggedUser = loggedUser;
            _repostory = repostory;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponseCategoriesJson> Execute ( CancellationToken cancellationToken )
        {
            var userLogged = await _loggedUser.Get();

            var cacheKey = CacheKeys.Categories(userLogged.Id);

            var cached = await _redisCacheService.GetAsync<ResponseCategoriesJson>(cacheKey);

            if( cached != null ) {
                return cached;
            }

            var result = await _repostory.GetAllAsync(userLogged , cancellationToken);

            var response =  new ResponseCategoriesJson
            {
                Categories = result.Select(category => category.ToShortResponse()).ToList()
            };

            await _redisCacheService.SetAsync(
                cacheKey ,
                response ,
                CacheExpiration.Category);



            return response;
        }
    }
}
