using MiCash.Application.UseCases.FinancialGols.Alert;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.FinancialGoals;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialGoals.Alerts
{
    public class GetFinancialGoalsAlertsUseCase
        : IGetFinancialGoalsAlertsUseCase
    {
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly IExpensesReadOnlyRepository _expenseReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialGoalCalculator _financialGoalCalculator;

        public GetFinancialGoalsAlertsUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            IExpensesReadOnlyRepository expenseReadOnlyRepository ,
            ILoggedUser loggedUser,
            IFinancialGoalCalculator financialGoalCalculator)
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _expenseReadOnlyRepository = expenseReadOnlyRepository;
            _loggedUser = loggedUser;
            _financialGoalCalculator = financialGoalCalculator;
        }

        public async Task<ResponseFinancialGoalsAlertsJson> Execute (
            int month ,
            int year ,
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var goals = await _financialGoalReadOnlyRepository
                .GetByPeriodAsync(
                    user.Id ,
                    month ,
                    year ,
                    cancellationToken);

            var expenses = await _expenseReadOnlyRepository
                .GetByPeriodAsync(
                    user.Id ,
                    month ,
                    year ,
                    cancellationToken);

            var expensesByCategory = expenses
                .GroupBy(expense => expense.CategoryId)
                .ToDictionary(
                    group => group.Key ,
                    group => group.Sum(expense => expense.Amount));

            var response = new ResponseFinancialGoalsAlertsJson();

            foreach ( var goal in goals ) {
                expensesByCategory.TryGetValue(
                    goal.CategoryId ,
                    out var currentAmount);

                var calculation =
                    _financialGoalCalculator
                        .Calculate(goal , currentAmount);


                if ( calculation.Status == FinancialGoalProgressStatus.Normal ) {
                    continue;
                }


                response.Alerts.Add(
                    new ResponseFinancialGoalAlertJson
                    {
                        CategoryId = goal.CategoryId ,

                        CategoryName = goal.Category.Name ,

                        TargetAmount = calculation.TargetAmount ,

                        CurrentAmount = calculation.CurrentAmount ,

                        DifferenceAmount = calculation.RemainingAmount ,

                        Status = ( int ) calculation.Status ,

                        StatusDescription = calculation.Status.ToString() ,

                        Message = GenerateMessage(
                            calculation.Status ,
                            goal.Category.Name ,
                            calculation.PercentageUsed ,
                            calculation.RemainingAmount)
                    });
            }

            return response;
        }

        private static string GenerateMessage (
            FinancialGoalProgressStatus status ,
            string categoryName ,
            decimal percentageUsed ,
            decimal remainingAmount )
        {
            return status switch
            {
                FinancialGoalProgressStatus.Warning =>
                    $"Você já utilizou {Math.Round(percentageUsed , 0)}% da sua meta de {categoryName}.",

                FinancialGoalProgressStatus.Exceeded =>
                    $"Você ultrapassou sua meta de {categoryName} em R$ {Math.Abs(remainingAmount):F2}.",

                _ => string.Empty
            };
        }
    }
}