using FluentValidation;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Exception;

namespace MiCash.Application.UseCases.FinancialGoals
{
    public class ValidatorUpdateFinancialGoal : AbstractValidator<RequestUpdateFinancialGoalJson>
    {
        public ValidatorUpdateFinancialGoal ()
        {
            RuleFor(x => x.TargetAmount)
                .GreaterThan(0).WithMessage(ResourceErrorMessages.TARGET_AMOUNT_MUST_BE_GREATER_THAN_ZERO);
        }
    }
}
