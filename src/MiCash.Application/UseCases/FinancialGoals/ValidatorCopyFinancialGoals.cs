using FluentValidation;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Exception;

namespace MiCash.Application.UseCases.FinancialGoals
{
    public class ValidatorCopyFinancialGoals : AbstractValidator<RequestCopyFinancialGoalsJson>
    {
        public ValidatorCopyFinancialGoals ()
        {
            RuleFor(x => x.SourceMonth)
                .InclusiveBetween(1 , 12).WithMessage(ResourceErrorMessages.MONTH_INVALID_RANGE);

            RuleFor(x => x.SourceYear)
                .InclusiveBetween(FinancialGoalPeriodValidator.MIN_YEAR , FinancialGoalPeriodValidator.MAX_YEAR)
                .WithMessage(ResourceErrorMessages.YEAR_INVALID);

            RuleFor(x => x.TargetMonth)
                .InclusiveBetween(1 , 12).WithMessage(ResourceErrorMessages.MONTH_INVALID_RANGE);

            RuleFor(x => x.TargetYear)
                .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Year).WithMessage(ResourceErrorMessages.YEAR_LESS_THAN_CURRENT)
                .LessThanOrEqualTo(FinancialGoalPeriodValidator.MAX_YEAR).WithMessage(ResourceErrorMessages.YEAR_INVALID);

            RuleFor(x => x)
                .Must(x => x.SourceMonth != x.TargetMonth || x.SourceYear != x.TargetYear)
                .WithMessage(ResourceErrorMessages.FINANCIAL_GOAL_SAME_PERIOD);
        }
    }
}
