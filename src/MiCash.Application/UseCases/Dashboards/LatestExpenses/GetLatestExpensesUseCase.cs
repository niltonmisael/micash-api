using MiCash.Communication.Responses.Dashboards;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Dashboards.LatestExpenses
{
    public class GetLatestExpensesUseCase : IGetLatestExpensesUseCase
    {
        private readonly IDashboardReadOnlyRepository _dashboardReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;

        public GetLatestExpensesUseCase (
            IDashboardReadOnlyRepository dashboardReadOnlyRepository,
            ILoggedUser loggedUser)
        {
            _dashboardReadOnlyRepository = dashboardReadOnlyRepository;
            _loggedUser = loggedUser;
        }

        public async Task<List<ResponseLatestExpenseJson>> Execute ( int count , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var expenses = await _dashboardReadOnlyRepository.GetLatestExpensesAsync( loggedUser.Id , count, cancellationToken);

            var result = expenses
                .Select(expense => new ResponseLatestExpenseJson
                {
                    Id = expense.Id ,
                    Title = expense.Title ,
                    Category = expense.Category ,
                    Color = expense.Color ,
                    Amount = expense.Amount ,
                    ExpenseDate = expense.ExpenseDate
                })
                .ToList();

            return result;
        }
    }
}
