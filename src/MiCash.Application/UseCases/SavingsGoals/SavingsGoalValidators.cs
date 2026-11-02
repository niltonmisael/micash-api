using FluentValidation;
using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Exception;

namespace MiCash.Application.UseCases.SavingsGoals
{
    internal static class SavingsGoalRules
    {
        public const int NAME_MAX_LENGTH = 100;

        public static void Name<T> ( IRuleBuilderInitial<T , string> rule ) =>
            rule.Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(ResourceErrorMessages.NAME_EMPTY)
                .MaximumLength(NAME_MAX_LENGTH).WithMessage(ResourceErrorMessages.SAVINGS_GOAL_NAME_TOO_LONG);

        // decimal(18,2) in the database: reject more than 2 decimal places instead of silently rounding.
        public static void PositiveMoney<T> ( IRuleBuilderInitial<T , decimal> rule , string message ) =>
            rule.Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithMessage(message)
                .PrecisionScale(18 , 2 , ignoreTrailingZeros: true).WithMessage(message);

        /// <summary>The deadline month (any day of it) cannot be before the current month.</summary>
        public static bool IsNotBeforeCurrentMonth ( DateOnly? deadline , DateOnly today ) =>
            deadline is null ||
            deadline.Value.Year * 12 + deadline.Value.Month >= today.Year * 12 + today.Month;
    }

    public class ValidatorRegisterSavingsGoal : AbstractValidator<RequestRegisterSavingsGoalJson>
    {
        public ValidatorRegisterSavingsGoal ( DateOnly today )
        {
            SavingsGoalRules.Name(RuleFor(x => x.Name));

            SavingsGoalRules.PositiveMoney(RuleFor(x => x.TargetAmount) , ResourceErrorMessages.SAVINGS_GOAL_TARGET_AMOUNT_INVALID);

            RuleFor(x => x.Deadline)
                .Must(deadline => SavingsGoalRules.IsNotBeforeCurrentMonth(deadline , today))
                .WithMessage(ResourceErrorMessages.SAVINGS_GOAL_DEADLINE_IN_PAST);

            RuleFor(x => x.InitialAmount!.Value)
                .Cascade(CascadeMode.Stop)
                .GreaterThanOrEqualTo(0).WithMessage(ResourceErrorMessages.SAVINGS_GOAL_INITIAL_AMOUNT_INVALID)
                .PrecisionScale(18 , 2 , ignoreTrailingZeros: true).WithMessage(ResourceErrorMessages.SAVINGS_GOAL_INITIAL_AMOUNT_INVALID)
                .When(x => x.InitialAmount is not null);

            RuleFor(x => x.Icon).NotEmpty().WithMessage(ResourceErrorMessages.ICON_REQUIRED);

            RuleFor(x => x.Color).NotEmpty().WithMessage(ResourceErrorMessages.COLOR_REQUIRED);
        }
    }

    /// <summary>
    /// The deadline rule is applied by the use case only when the deadline changes: a goal whose
    /// deadline already passed must still be editable (e.g. renamed) without forcing a new deadline.
    /// </summary>
    public class ValidatorUpdateSavingsGoal : AbstractValidator<RequestUpdateSavingsGoalJson>
    {
        public ValidatorUpdateSavingsGoal ()
        {
            SavingsGoalRules.Name(RuleFor(x => x.Name));

            SavingsGoalRules.PositiveMoney(RuleFor(x => x.TargetAmount) , ResourceErrorMessages.SAVINGS_GOAL_TARGET_AMOUNT_INVALID);

            RuleFor(x => x.Icon).NotEmpty().WithMessage(ResourceErrorMessages.ICON_REQUIRED);

            RuleFor(x => x.Color).NotEmpty().WithMessage(ResourceErrorMessages.COLOR_REQUIRED);
        }
    }

    public class ValidatorSavingsMovement : AbstractValidator<RequestSavingsMovementJson>
    {
        public ValidatorSavingsMovement ( DateOnly today )
        {
            SavingsGoalRules.PositiveMoney(RuleFor(x => x.Amount) , ResourceErrorMessages.SAVINGS_MOVEMENT_AMOUNT_INVALID);

            // DateOnly defaults to 0001-01-01 when the app omits the field.
            RuleFor(x => x.Date)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(ResourceErrorMessages.SAVINGS_MOVEMENT_DATE_REQUIRED)
                .LessThanOrEqualTo(today).WithMessage(ResourceErrorMessages.SAVINGS_MOVEMENT_DATE_IN_FUTURE);
        }
    }
}
