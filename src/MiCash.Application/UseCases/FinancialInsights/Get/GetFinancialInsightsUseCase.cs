using DocumentFormat.OpenXml.Bibliography;
using MiCash.Communication.Requests.FinancialInsights;
using MiCash.Communication.Responses.FinancialInsights;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.FinancialInsights;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialInsights.Get
{
    public class GetFinancialInsightsUseCase
        : IGetFinancialInsightsUseCase
    {
        private readonly IExpensesReadOnlyRepository _expenseReadOnlyRepository;
        private readonly IIncomesReadOnlyRepository _incomeReadOnlyRepository;
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly IFinancialInsightAnalyzer _financialInsightAnalyzer;
        private readonly ILoggedUser _loggedUser;
        private readonly IRedisCacheService _redisCacheService;

        public GetFinancialInsightsUseCase (
            IExpensesReadOnlyRepository expenseReadOnlyRepository ,
            IIncomesReadOnlyRepository incomeReadOnlyRepository ,
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            IFinancialInsightAnalyzer financialInsightAnalyzer ,
            ILoggedUser loggedUser,
            IRedisCacheService redisCacheService)
        {
            _expenseReadOnlyRepository = expenseReadOnlyRepository;
            _incomeReadOnlyRepository = incomeReadOnlyRepository;
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _financialInsightAnalyzer = financialInsightAnalyzer;
            _loggedUser = loggedUser;
            _redisCacheService = redisCacheService;
        }

        public async Task<ResponseFinancialInsightsJson> Execute (
            RequestFinancialInsightJson request ,
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var cacheKey = CacheKeys.FinancialInsights(
                user.Id ,
                request.Month ,
                request.Year);

            var cachedResponse =
                await _redisCacheService
                    .GetAsync<ResponseFinancialInsightsJson>(
                        cacheKey);

            if ( cachedResponse is not null ) {
                return cachedResponse;
            }

            var expenses =
                await _expenseReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        request.Month ,
                        request.Year ,
                        cancellationToken);

            var incomes =
                await _incomeReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        request.Month ,
                        request.Year ,
                        cancellationToken);

            var financialGoals =
                await _financialGoalReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        request.Month ,
                        request.Year ,
                        cancellationToken);

            var analysisData = new FinancialAnalysisData
            {
                Expenses = expenses ,
                Incomes = incomes ,
                FinancialGoals = financialGoals
            };

            var insights =
                _financialInsightAnalyzer
                    .Analyze(analysisData);

            var response = new ResponseFinancialInsightsJson
            {
                Insights = insights
                    .Select(insight =>
                        new ResponseFinancialInsightJson
                        {
                            Type = ( int ) insight.Type ,

                            TypeDescription = insight.Type.ToString() ,

                            Title = insight.Title ,

                            Message = insight.Message
                        })
                    .ToList()
            };

            await _redisCacheService.SetAsync(
                cacheKey ,
                response ,
                TimeSpan.FromMinutes(1));

            return response;
        }
    }
}