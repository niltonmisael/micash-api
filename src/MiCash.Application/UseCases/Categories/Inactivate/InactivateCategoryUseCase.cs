using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Categories.Inactivate
{
    public class InactivateCategoryUseCase : IInactivateCategoryUseCase
    {
        private readonly ICategoriesReadOnlyRepository _categoriesReadOnlyRepository;
        private readonly ICategoriesUpdateOnlyRepository _categoriesUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggedUser _loggedUser;

        public InactivateCategoryUseCase (
            ICategoriesReadOnlyRepository categoriesReadOnlyRepository,
            ICategoriesUpdateOnlyRepository categoriesUpdateOnlyRepository ,
            IUnitOfWork unitOfWork,
            ILoggedUser loggedUser)
        {
            _categoriesReadOnlyRepository = categoriesReadOnlyRepository;
            _categoriesUpdateOnlyRepository = categoriesUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _loggedUser = loggedUser;
        }

        public async Task Execute ( long id , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var category = await _categoriesReadOnlyRepository.GetByIdAsync(loggedUser , id , cancellationToken);

            if ( category == null ) {
                throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);
            }

            if(!category.IsActive ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.CATEGORY_ALREADY_INACTIVE]);
            }

            await _categoriesUpdateOnlyRepository.InactivateAsync(category.Id ,  cancellationToken);

            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
