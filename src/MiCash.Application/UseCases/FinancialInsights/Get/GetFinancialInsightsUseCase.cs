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
using MiCash.Domain.Services.Plans;
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
    private readonly IUserPlanService _userPlanService;

    public GetFinancialInsightsUseCase (
        IExpensesReadOnlyRepository expenseReadOnlyRepository ,
        IIncomesReadOnlyRepository incomeReadOnlyRepository ,
        IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
        IFinancialInsightAnalyzer financialInsightAnalyzer ,
        IFinancialInsightPromptBuilder financialInsightPromptBuilder ,
        IAIService aiService ,
        ILoggedUser loggedUser ,
        IRedisCacheService redisCacheService ,
        ILogger<GetFinancialInsightsUseCase> logger ,
        IUserPlanService userPlanService )
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
        _userPlanService = userPlanService;
    }

    public async Task<ResponseFinancialInsightsJson> Execute (
        RequestFinancialInsightJson request ,
        CancellationToken cancellationToken )
    {
        PeriodValidator.Validate(request.Month , request.Year);

        var user = await _loggedUser.Get();

        var limits = await _userPlanService.GetLimitsAsync(user , cancellationToken);

        var cacheKey = CacheKeys.FinancialInsights(
            user.Id ,
            request.Month ,
            request.Year);

        // The cache only holds the full (Premium) response; the Free preview is cheap and never cached,
        // so an upgrade shows the full analysis at once.
        if ( limits.FullInsights ) {
            var cachedResponse =
                await _redisCacheService
                    .GetAsync<ResponseFinancialInsightsJson>(cacheKey);

            if ( cachedResponse is not null ) {
                return cachedResponse;
            }
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

        if ( limits.FullInsights == false ) {
            return ToPreview(response);
        }

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

        // Only cache the complete response: if the AI failed (transient outage), the next
        // request should try again instead of serving a response without the AI analysis
        // for the whole cache window. The circuit breaker protects the provider meanwhile.
        if ( response.ArtificialInsight is not null ) {
            await _redisCacheService.SetAsync(
                cacheKey ,
                response ,
                CacheExpiration.FinancialInsights);
        }

        return response;
    }

    /// <summary>
    /// Free plan: only the first insight, no AI analysis (no AI cost). The app shows the rest as locked.
    /// </summary>
    private static ResponseFinancialInsightsJson ToPreview ( ResponseFinancialInsightsJson full ) => new()
    {
        Insights = full.Insights.Take(1).ToList() ,
        IsPreview = true ,
        // The hidden rule-based insights plus the AI analysis.
        LockedInsightsCount = Math.Max(full.Insights.Count - 1 , 0) + 1
    };
}