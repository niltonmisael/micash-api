using FluentValidation;
using MiCash.Communication.Requests.Chat;
using MiCash.Exception;

namespace MiCash.Application.UseCases.FinancialChat
{
    public class ValidatorChatMessage : AbstractValidator<RequestChatMessageJson>
    {
        public const int MAX_LENGTH = 1000;

        public ValidatorChatMessage ()
        {
            RuleFor(x => x.Message)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(ResourceErrorMessages.CHAT_MESSAGE_REQUIRED)
                .MaximumLength(MAX_LENGTH).WithMessage(ResourceErrorMessages.CHAT_MESSAGE_TOO_LONG);
        }
    }
}
