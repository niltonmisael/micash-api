using FluentValidation;
using MiCash.Communication.Requests;
using MiCash.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Users.Update
{
    public class UpdateUserValidator : AbstractValidator<RequestUpdateUserJson>
    {
        public UpdateUserValidator ()
        {
            RuleFor(user => user.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_EMPTY);
            RuleFor(user => user.Email)
                .NotEmpty()
                .WithMessage(ResourceErrorMessages.EMAIL_EMPTY)
                .EmailAddress()
                .When(user => string.IsNullOrWhiteSpace(user.Email) == false , ApplyConditionTo.CurrentValidator)
                .WithMessage(ResourceErrorMessages.EMAIL_INVALID);
            // Null keeps the current phone; when sent, it must be a valid mobile.
            RuleFor(user => user.Phone!)
                .Must(PhoneRules.IsValidMobile)
                .When(user => user.Phone is not null)
                .WithMessage(ResourceErrorMessages.PHONE_INVALID);
        }
    }
}
