using MiCash.Application.AI.PromptBuilders;
using MiCash.Communication.Requests.FinancialInsights;
using MiCash.Communication.Responses.FinancialInsights;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.FinancialInsights;
using MiCash.Domain.Services.IA;
using MiCash.Domain.Services.LoggedUser;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.FinancialInsights.Get;

public class GetFinancialInsightsUseCase : IGetFinancialInsightsUseCase
{
    private readonly IExpensesReadOnlyRepository _expenseReadOnlyRepository;
    private readonly IIncomesReadOnlyRepository _incomeReadOnlyRepository;
    private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
    private readonly IFinancialInsightAnalyzer _financialInsightAnalyzer;
    private readonly IFinancialInsightPromptBuilder _financialInsightPromptBuilder;
    private readonly IAIService _aiService;
    private readonly ILoggedUser _loggedUser;
    private readonly IRedisCacheService _redisCacheService;
    private readonly ILogger<GetFinancialInsightsUseCase> _logger;

    public GetFinancialInsightsUseCase (
        IExpensesReadOnlyRepository expenseReadOnlyRepository ,
        IIncomesReadOnlyRepository incomeReadOnlyRepository ,
        IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
        IFinancialInsightAnalyzer financialInsightAnalyzer ,
        IFinancialInsightPromptBuilder financialInsightPromptBuilder ,
        IAIService aiService ,
        ILoggedUser loggedUser ,
        IRedisCacheService redisCacheService ,
        ILogger<GetFinancialInsightsUseCase> logger )
    {
        _expenseReadOnlyRepository = expenseReadOnlyRepository;
        _incomeReadOnlyRepository = incomeReadOnlyRepository;
        _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
        _financialInsightAnalyzer = financialInsightAnalyzer;
        _financialInsightPromptBuilder = financialInsightPromptBuilder;
        _aiService = aiService;
        _loggedUser = loggedUser;
        _redisCacheService = redisCacheService;
        _logger = logger;
    }

    public async Task<ResponseFinancialInsightsJson> Execute (
        RequestFinancialInsightJson request ,
        CancellationToken cancellationToken )
    {
        PeriodValidator.Validate(request.Month , request.Year);

        var user = await _loggedUser.Get();

        var cacheKey = CacheKeys.FinancialInsights(
            user.Id ,
            request.Month ,
            request.Year);

        var cachedResponse =
            await _redisCacheService
                .GetAsync<ResponseFinancialInsightsJson>(cacheKey);

        if ( cachedResponse is not null ) {
            return cachedResponse;
        }

        // DbContext is not thread-safe: queries must run sequentially.
        var expenses =
            await _expenseReadOnlyRepository.GetByPeriodAsync(
                user.Id ,
                request.Month ,
                request.Year ,
                cancellationToken);

        var incomes =
            await _incomeReadOnlyRepository.GetByPeriodAsync(
                user.Id ,
                request.Month ,
                request.Year ,
                cancellationToken);

        var goals =
            await _financialGoalReadOnlyRepository.GetByPeriodAsync(
                user.Id ,
                request.Month ,
                request.Year ,
                cancellationToken);

        var analysisData = new FinancialAnalysisData
        {
            Expenses = expenses ,
            Incomes = incomes ,
            FinancialGoals = goals
        };

        var domainInsights =
            _financialInsightAnalyzer
                .Analyze(analysisData);

        var response = new ResponseFinancialInsightsJson
        {
            Insights = domainInsights
                .Select(insight => new ResponseFinancialInsightJson
                {
                    Type = ( int ) insight.Type ,
                    TypeDescription = insight.Type.ToString() ,
                    Title = insight.Title ,
                    Message = insight.Message
                })
                .ToList()
        };

        try {
            var aiRequest =
                _financialInsightPromptBuilder
                    .BuildFinancialInsights(analysisData);

            var aiResponse =
                await _aiService.GenerateResponseAsync(
                    aiRequest ,
                    cancellationToken);

            response.ArtificialInsight =
                new ResponseArtificialInsightJson
                {
                    Title = "Análise da MI" ,
                    Message = aiResponse.Content
                };
        } catch ( System.Exception ex ) {
            _logger.LogWarning(
                ex ,
                "Erro ao gerar insights utilizando a MI.");
        }

        await _redisCacheService.SetAsync(
            cacheKey ,
            response ,
            CacheExpiration.FinancialInsights);

        return response;
    }
}