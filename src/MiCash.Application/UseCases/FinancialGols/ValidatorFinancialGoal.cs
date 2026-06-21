using FluentValidation;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Exception;

namespace MiCash.Application.UseCases.FinancialGols
{
    public class ValidatorFinancialGoal : AbstractValidator<RequestRegisterFinancialGoal>
    {
        public ValidatorFinancialGoal ()
        {

            RuleFor(x => x.TargetAmount)
                .NotEmpty().WithMessage(ResourceErrorMessages.TARGET_AMOUNT_IS_REQUIRED)
                .GreaterThan(0).WithMessage(ResourceErrorMessages.TARGET_AMOUNT_MUST_BE_GREATER_THAN_ZERO);

            RuleFor(x => x.Month)
                .NotEmpty().WithMessage(ResourceErrorMessages.MONTH_IS_REQUIRED)
                .InclusiveBetween(1 , 12).WithMessage(ResourceErrorMessages.MONTH_INVALID_RANGE);

            RuleFor(x => x.Year)
                .NotEmpty().WithMessage(ResourceErrorMessages.YEAR_IS_REQUIRED)
                .GreaterThanOrEqualTo(DateTime.UtcNow.Year).WithMessage(ResourceErrorMessages.YEAR_LESS_THAN_CURRENT);
        }
    }
    
}
