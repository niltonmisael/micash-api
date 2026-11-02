using FluentValidation;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Exception;

namespace MiCash.Application.UseCases.FinancialGoals
{
    public class ValidatorFinancialGoal : AbstractValidator<RequestRegisterFinancialGoal>
    {
        public ValidatorFinancialGoal ()
        {
            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage(ResourceErrorMessages.CATEGORY_NOT_FOUND);

            RuleFor(x => x.TargetAmount)
                .GreaterThan(0).WithMessage(ResourceErrorMessages.TARGET_AMOUNT_MUST_BE_GREATER_THAN_ZERO);

            RuleFor(x => x.Month)
                .InclusiveBetween(1 , 12).WithMessage(ResourceErrorMessages.MONTH_INVALID_RANGE);

            RuleFor(x => x.Year)
                .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Year).WithMessage(ResourceErrorMessages.YEAR_LESS_THAN_CURRENT)
                .LessThanOrEqualTo(PeriodValidator.MAX_YEAR).WithMessage(ResourceErrorMessages.YEAR_INVALID);
        }
    }
}
