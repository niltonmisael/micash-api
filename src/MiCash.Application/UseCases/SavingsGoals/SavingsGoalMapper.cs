using FluentValidation.Results;
using MiCash.Communication.Responses.SavingsGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Services.SavingsGoals;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.SavingsGoals
{
    internal static class SavingsGoalMapper
    {
        public static ResponseShortSavingsGoalJson ToShort ( SavingsGoal goal , decimal savedAmount , DateOnly today )
        {
            var summary = SavingsGoalCalculator.Calculate(goal.TargetAmount , savedAmount , goal.Deadline , today);

            return new ResponseShortSavingsGoalJson
            {
                Id = goal.Id ,
                Name = goal.Name ,
                Icon = goal.Icon ,
                Color = goal.Color ,
                TargetAmount = goal.TargetAmount ,
                SavedAmount = summary.SavedAmount ,
                Percentage = summary.Percentage ,
                Deadline = goal.Deadline ,
                IsCompleted = summary.IsCompleted
            };
        }

        public static ResponseSavingsGoalJson ToDetail ( SavingsGoal goal , DateOnly today )
        {
            var saved = goal.Movements.Sum(Signed);

            var summary = SavingsGoalCalculator.Calculate(goal.TargetAmount , saved , goal.Deadline , today);

            return new ResponseSavingsGoalJson
            {
                Id = goal.Id ,
                Name = goal.Name ,
                Icon = goal.Icon ,
                Color = goal.Color ,
                TargetAmount = goal.TargetAmount ,
                SavedAmount = summary.SavedAmount ,
                RemainingAmount = summary.RemainingAmount ,
                Percentage = summary.Percentage ,
                Deadline = goal.Deadline ,
                MonthlyAmountNeeded = summary.MonthlyAmountNeeded ,
                IsCompleted = summary.IsCompleted ,
                Movements = goal.Movements
                    .OrderByDescending(m => m.Date)
                    .ThenByDescending(m => m.CreatedAt)
                    .ThenByDescending(m => m.Id)
                    .Select(m => new ResponseSavingsMovementJson
                    {
                        Id = m.Id ,
                        Type = ( int ) m.Type ,
                        Amount = m.Amount ,
                        Date = m.Date
                    })
                    .ToList()
            };
        }

        public static decimal Signed ( SavingsGoalMovement movement ) =>
            movement.Type == SavingsMovementType.Deposit ? movement.Amount : -movement.Amount;

        public static void ThrowIfInvalid ( ValidationResult result )
        {
            if ( result.IsValid == false ) {
                throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
            }
        }
    }
}
