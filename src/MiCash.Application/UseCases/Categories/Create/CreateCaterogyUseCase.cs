using MiCash.Application.Mappers;
using MiCash.Communication.Requests.Categories;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Categories.Create
{
    public class CreateCaterogyUseCase : ICreateCaterogyUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly ICategoriesWriteOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisCacheService _redisCacheService;

        public CreateCaterogyUseCase (
            ILoggedUser loggedUser,
            ICategoriesWriteOnlyRepository repository,
            IUnitOfWork unitOfWork,
            IRedisCacheService redisCacheService)
        {
            _loggedUser = loggedUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _redisCacheService = redisCacheService;
        }
        public async Task CreateCaterogy ( 
            RequestCreateCategoryJson request , 
            CancellationToken cancellationToken )
        {
            Validate(request);

            var loggedUser = await _loggedUser.Get();

            var category = request.ToEntity();
            category.UserId = loggedUser.Id;

            await _repository.Create(category);
            await _unitOfWork.Commit(cancellationToken);

            await _redisCacheService.RemoveAsync(
                CacheKeys.Categories(loggedUser.Id));

        }

        private void Validate ( RequestCreateCategoryJson request )
        {
            var validator = new CategoryValidator();

            var result = validator.Validate(request);

            if ( result.IsValid == false ) {

                var errorMessage = result.Errors.Select(f => f.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessage);

            }
        }
    }
}
