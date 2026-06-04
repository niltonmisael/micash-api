using FluentValidation;
using MiCash.Communication.Requests.Incomes;
using MiCash.Exception;

namespace MiCash.Application.UseCases.Incomes
{
    public class IncomeValidator : AbstractValidator<RequestIncomeJson>
    {
        public IncomeValidator ()
        {
            RuleFor(income => income.Title).NotEmpty().WithMessage(ResourceErrorMessages.TITLE_REQUIRED);
            RuleFor(income => income.Amount).GreaterThan(0).WithMessage(ResourceErrorMessages.AMOUNT_MUST_BE_GREATER_THAN_ZERO);
            RuleFor(income => income.ReceivedAt).NotEmpty().WithMessage(ResourceErrorMessages.RECEIVED_DATE_IS_REQUIRED);
            RuleFor(income => income.IncomeCategory).IsInEnum().WithMessage(ResourceErrorMessages.INCOME_TYPE_IS_INVALID);
        }
    }
}
