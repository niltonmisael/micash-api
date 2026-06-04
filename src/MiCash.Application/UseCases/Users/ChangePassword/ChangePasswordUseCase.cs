using FluentValidation.Results;
using MiCash.Communication.Requests;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Users.ChangePassword
{
    public class ChangePasswordUseCase : IChangePasswordUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserUpdateOnlyRepository _userUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordEncripter _passwordEncripter;

        public ChangePasswordUseCase (
            ILoggedUser loggedUser, 
            IUserUpdateOnlyRepository userUpdateOnlyRepository, 
            IUnitOfWork unitOfWork, 
            IPasswordEncripter passwordEncripter)
        {
            _loggedUser = loggedUser;   
            _userUpdateOnlyRepository = userUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _passwordEncripter = passwordEncripter;
        }
        public async Task Execute ( RequestChangePasswordJson request, CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();
            Validate(request, loggedUser);

            var user = await _userUpdateOnlyRepository.GetById(loggedUser.Id);
            user.PasswordHash = _passwordEncripter.Encrypt(request.NewPassword);

            _userUpdateOnlyRepository.Update(user);

            await _unitOfWork.Commit(cancellationToken);

        }

        private void Validate ( RequestChangePasswordJson request , User loggedUser)
        {
            var validator = new ChangePasswordValidator();

            var result = validator.Validate(request);

            var passwordMatch = _passwordEncripter.Verify(request.Password, loggedUser.PasswordHash);

            if(passwordMatch == false ) {
                result.Errors.Add(new ValidationFailure(string.Empty , ResourceErrorMessages.PASSWORD_DIFFERENT_CURRENT_PASSWORD));
            }

            if ( result.IsValid == false ) {
                var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errors);
            }
        }
    }
}
