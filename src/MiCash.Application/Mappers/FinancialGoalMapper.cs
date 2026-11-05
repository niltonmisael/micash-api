using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Entities;

namespace MiCash.Application.Mappers
{
    internal static class FinancialGoalMapper
    {
        public static FinancialGoal ToEntity ( this RequestRegisterFinancialGoal request ) => new()
        {
            CategoryId = request.CategoryId ,
            TargetAmount = request.TargetAmount ,
            Month = request.Month ,
            Year = request.Year
        };

        public static ResponseFinancialGoalJson ToResponse ( this FinancialGoal goal ) => new()
        {
            Id = goal.Id ,
            CategoryId = goal.CategoryId ,
            CategoryName = goal.Category?.Name ?? string.Empty ,
            TargetAmount = goal.TargetAmount ,
            Month = goal.Month ,
            Year = goal.Year
        };

        public static ResponseRegisteredFinancialGoalJson ToRegisteredResponse ( this FinancialGoal goal ) => new()
        {
            Id = goal.Id
        };
    }
}
