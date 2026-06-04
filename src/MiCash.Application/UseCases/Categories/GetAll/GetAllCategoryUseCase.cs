using AutoMapper;
using MiCash.Communication.Responses.Categories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Categories.GetAll
{
    public class GetAllCategoryUseCase : IGetAllCategoryUseCase
    {
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;
        private readonly ICategoriesReadOnlyRepository _repostory;

        public GetAllCategoryUseCase (
            IMapper mapper,
            ILoggedUser loggedUser,
            ICategoriesReadOnlyRepository repostory)
        {
            _mapper = mapper;
            _loggedUser = loggedUser;
            _repostory = repostory;
        }
        public async Task<ResponseCategoryJson> Execute ( CancellationToken cancellationToken )
        {
            var userLogged = await _loggedUser.Get();

            var result = await _repostory.GetAllAsync(userLogged , cancellationToken);

            return new ResponseCategoryJson
            {
                Categories = _mapper.Map<IEnumerable<ResponseShortCategoryJson>>(result)
            };
        }
    }
}
