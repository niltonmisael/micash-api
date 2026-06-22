using MiCash.Communication.Requests.Categories;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Categories.Update
{
    public class UpdateCategoryUseCase : IUpdateCategoryUseCase
    {
        private readonly ICategoriesReadOnlyRepository _categoriesReadOnlyRepository;
        private readonly ICategoriesUpdateOnlyRepository _categoriesUpdateOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisCacheService _redisCacheService;

        public UpdateCategoryUseCase (
            ICategoriesReadOnlyRepository categoriesReadOnlyRepository,
            ICategoriesUpdateOnlyRepository categoriesUpdateOnlyRepository,
            ILoggedUser loggedUser,
            IUnitOfWork unitOfWork,
            IRedisCacheService redisCacheService)
        {
            _categoriesReadOnlyRepository = categoriesReadOnlyRepository;
            _categoriesUpdateOnlyRepository = categoriesUpdateOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _redisCacheService = redisCacheService;
        }
        public async Task Execute ( long categoryId , RequestUpdateCategoryJson request , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var category = await _categoriesReadOnlyRepository.GetByIdAsync( loggedUser , categoryId, cancellationToken);

            if ( category == null ) {
                throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);
            }

            await _categoriesUpdateOnlyRepository.UpdateAsync(categoryId, request.Name, request.Color, request.Icon, cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            await _redisCacheService.RemoveAsync(
                CacheKeys.Categories(loggedUser.Id));

            await _redisCacheService.RemoveAsync(
                CacheKeys.CategoryById(loggedUser.Id , category.Id));
        }

    }
}
