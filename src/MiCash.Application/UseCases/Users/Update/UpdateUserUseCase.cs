using MiCash.Communication.Requests;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Users.Update
{
    /// <summary>
    /// PUT api/user/update: updates the name and, when sent, the phone. The e-mail field must be the current e-mail; a different one is
    /// 400 EMAIL_CHANGE_REQUIRES_CONFIRMATION, because the address only changes after a code sent to it
    /// (POST api/user/email-change → POST api/user/email-change/confirm).
    /// </summary>
    public class UpdateUserUseCase : IUpdateUserUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserUpdateOnlyRepository _userUpdateOnlyRepository;

        public UpdateUserUseCase (
            ILoggedUser loggedUser,
            IUnitOfWork unitOfWork,
            IUserUpdateOnlyRepository userUpdateOnlyRepository)
        {
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _userUpdateOnlyRepository = userUpdateOnlyRepository;
        }

        public async Task Execute ( RequestUpdateUserJson request, CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            Validate(request , loggedUser.Email);

            var user = await _userUpdateOnlyRepository.GetById(loggedUser.Id);

            user.Name = request.Name;
            if ( request.Phone is not null ) {
                user.Phone = PhoneRules.Digits(request.Phone);
            }
            user.UpdatedAt = DateTime.UtcNow;

            _userUpdateOnlyRepository.Update(user);

            await _unitOfWork.Commit(cancellationToken);
        }

        private static void Validate ( RequestUpdateUserJson request, string currentEmail )
        {
            var result = new UpdateUserValidator().Validate(request);

            if ( result.IsValid == false ) {

                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessages);
            }

            if ( request.Email.Trim() != currentEmail ) {
                throw new EmailChangeRequiresConfirmationException();
            }
        }
    }
}
