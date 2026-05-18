using FluentValidation.Results;
using MiCash.Communication.Requests;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Users.Update
{
    public class UpdateUserUseCase : IUpdateUserUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserReadOnlyRepository _userReadOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserUpdateOnlyRepository _userUpdateOnlyRepository;

        public UpdateUserUseCase (
            ILoggedUser loggedUser, 
            IUserReadOnlyRepository userReadOnlyRepository, 
            IUnitOfWork unitOfWork, 
            IUserUpdateOnlyRepository userUpdateOnlyRepository)
        {
            _loggedUser = loggedUser;
            _userReadOnlyRepository = userReadOnlyRepository;
            _unitOfWork = unitOfWork;
            _userUpdateOnlyRepository = userUpdateOnlyRepository;
        }
        public async Task Execute ( RequestUpdateUserJson request )
        {
            var loggedUser = await _loggedUser.Get();

            await Validate(request , loggedUser.Email);

            var user = await _userUpdateOnlyRepository.GetById(loggedUser.Id);

            user.Name = request.Name;
            user.Email = request.Email;

            _userUpdateOnlyRepository.Update(user);

            await _unitOfWork.Commit();
        }


        private async Task Validate( RequestUpdateUserJson request, string currentEmail )
        {
            var validator = new UpdateUserValidator();

            var result = validator.Validate(request);

            if(currentEmail.Equals(request.Email) == false) {

                var userExist = await _userReadOnlyRepository.ExistActiveUserWithEmail(request.Email);
                if(userExist) {
                    result.Errors.Add(new ValidationFailure(string.Empty , ResourceErrorMessages.EMAIL_ALREADY_REGISTERED));
                }
            }

            if ( result.IsValid == false ) {

                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessages);
            }
        }
    }
}
