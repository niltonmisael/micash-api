using AutoMapper;
using MiCash.Communication.Requests.Categories;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Categories.Create
{
    public class CreateCaterogyUseCase : ICreateCaterogyUseCase
    {
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;
        private readonly ICategoriesWriteOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCaterogyUseCase (
            IMapper mapper,
            ILoggedUser loggedUser,
            ICategoriesWriteOnlyRepository repository,
            IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _loggedUser = loggedUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public async Task CreateCaterogy ( 
            RequestCreateCategoryJson request , 
            CancellationToken cancellationToken )
        {
            Validate(request);

            var loggedUser = await _loggedUser.Get();

            var category = _mapper.Map<Domain.Entities.Category>(request);
            category.UserId = loggedUser.Id;

            await _repository.Create(category);
            await _unitOfWork.Commit();
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
