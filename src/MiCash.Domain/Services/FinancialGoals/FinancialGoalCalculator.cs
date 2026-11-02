using MiCash.Domain.Entities;
using MiCash.Domain.Enums;

namespace MiCash.Domain.Services.FinancialGoals
{
    public class FinancialGoalCalculator : IFinancialGoalCalculator
    {
        public FinancialGoalCalculation Calculate (
            FinancialGoal goal ,
            decimal currentAmount )
        {
            var percentageUsed = goal.TargetAmount == 0
                ? 0
                : ( currentAmount / goal.TargetAmount ) * 100;

            var status = GetStatus(percentageUsed);

            return new FinancialGoalCalculation
            {
                CurrentAmount = currentAmount ,

                TargetAmount = goal.TargetAmount ,

                RemainingAmount =
                    goal.TargetAmount - currentAmount ,

                PercentageUsed =
                    Math.Round(percentageUsed , 2) ,

                Status = status
            };
        }


        private static FinancialGoalProgressStatus GetStatus (
            decimal percentageUsed )
        {
            if ( percentageUsed >= 100 ) {
                return FinancialGoalProgressStatus.Exceeded;
            }

            if ( percentageUsed >= 80 ) {
                return FinancialGoalProgressStatus.Warning;
            }

            return FinancialGoalProgressStatus.Normal;
        }
    }
}
