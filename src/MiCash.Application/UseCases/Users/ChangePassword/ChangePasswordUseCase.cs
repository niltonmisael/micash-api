using FluentValidation.Results;
using MiCash.Application.UseCases.Auth;
using MiCash.Domain.Services.Cache;
using Microsoft.Extensions.Logging;
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
        private readonly IRedisCacheService _cache;
        private readonly TimeProvider _time;
        private readonly ILogger<ChangePasswordUseCase> _logger;

        public ChangePasswordUseCase (
            ILoggedUser loggedUser,
            IUserUpdateOnlyRepository userUpdateOnlyRepository,
            IUnitOfWork unitOfWork,
            IPasswordEncripter passwordEncripter,
            IRedisCacheService cache,
            TimeProvider time,
            ILogger<ChangePasswordUseCase> logger)
        {
            _loggedUser = loggedUser;
            _userUpdateOnlyRepository = userUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _passwordEncripter = passwordEncripter;
            _cache = cache;
            _time = time;
            _logger = logger;
        }
        public async Task Execute ( RequestChangePasswordJson request, CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();
            Validate(request, loggedUser);

            var now = _time.GetUtcNow().UtcDateTime;

            var user = await _userUpdateOnlyRepository.GetById(loggedUser.Id);
            user.PasswordHash = _passwordEncripter.Encrypt(request.NewPassword);
            user.UpdatedAt = now;

            _userUpdateOnlyRepository.Update(user);

            await _unitOfWork.Commit(cancellationToken);

            // Same as the password reset: sessions opened before the change can no longer be renewed.
            await SessionRevocation.RevokeOlderSessionsAsync(_cache , user , now , _logger);
        }

        private void Validate ( RequestChangePasswordJson request , User loggedUser)
        {
            var validator = new ChangePasswordValidator();

            var result = validator.Validate(request);

            var passwordMatch = _passwordEncripter.Verify(request.Password, loggedUser.PasswordHash);

            if(passwordMatch == false ) {
                result.Errors.Add(new ValidationFailure(string.Empty , ResourceErrorMessages.PASSWORD_DIFFERENT_CURRENT_PASSWORD));
            } else if ( request.NewPassword == request.Password ) {
                // The current password was just confirmed, so comparing the texts is enough (no second hash check).
                result.Errors.Add(new ValidationFailure(string.Empty , ResourceErrorMessages.NEW_PASSWORD_SAME_AS_CURRENT));
            }

            if ( result.IsValid == false ) {
                var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errors);
            }
        }
    }
}
