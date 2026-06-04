using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Users.Delete
{
    public class DeleteUserAccountUseCase : IDeleteUserAccountUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteUserAccountUseCase (
            ILoggedUser loggedUser, 
            IUserWriteOnlyRepository userWriteOnlyRepository, 
            IUnitOfWork unitOfWork)
        {
            _loggedUser = loggedUser;
            _userWriteOnlyRepository = userWriteOnlyRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Execute (CancellationToken cancellationToken)
        {
            var user = await _loggedUser.Get();

            await _userWriteOnlyRepository.Delete(user);

            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
