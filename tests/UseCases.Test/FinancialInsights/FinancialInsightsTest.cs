using CommonTestUtilities.AI;
using CommonTestUtilities.Cache;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using FluentAssertions;
using MiCash.Application.AI.PromptBuilders;
using MiCash.Application.UseCases.FinancialInsights.Get;
using MiCash.Communication.Requests.FinancialInsights;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.FinancialInsights;
using MiCash.Exception.ExceptionBase;
using MiCash.Exception.ExceptionBase.IA;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UseCases.Test.FinancialInsights
{
    internal static class InsightScenario
    {
        public const int MONTH = 6;
        public static readonly int YEAR = DateTime.UtcNow.Year;

        // Two "Mercado" expenses with distinct Category instances (as returned by AsNoTracking queries),
        // one "Lazer" expense, a goal for Mercado (exceeded) and income lower than expenses.
        public static FinancialAnalysisData Build ( User user )
        {
            Category Mercado () => new() { Id = 1 , Name = "Mercado" , UserId = user.Id };
            var lazer = new Category { Id = 2 , Name = "Lazer" , UserId = user.Id };

            return new FinancialAnalysisData
            {
                Expenses =
                [
                    new Expense { UserId = user.Id , CategoryId = 1 , Category = Mercado() , Amount = 300 },
                    new Expense { UserId = user.Id , CategoryId = 1 , Category = Mercado() , Amount = 300 },
                    new Expense { UserId = user.Id , CategoryId = 2 , Category = lazer , Amount = 400 },
                ],
                Incomes =
                [
                    new Income { UserId = user.Id , Amount = 900 }
                ],
                FinancialGoals =
                [
                    new FinancialGoal { UserId = user.Id , CategoryId = 1 , Category = Mercado() , TargetAmount = 500 , Month = MONTH , Year = YEAR }
                ]
            };
        }
    }

    public class FinancialInsightAnalyzerTest
    {
        [Fact]
        public void Groups_Same_Category_Even_With_Distinct_Instances ()
        {
            var data = InsightScenario.Build(UserBuilder.Build());

            var insights = new FinancialInsightAnalyzer().Analyze(data);

            insights.Should().ContainSingle(i => i.Title == "Maior gasto do mês")
                .Which.Message.Should().StartWith("Mercado representou 60");
        }

        [Fact]
        public void Detects_Exceeded_Goal_And_Negative_Balance ()
        {
            var data = InsightScenario.Build(UserBuilder.Build());

            var insights = new FinancialInsightAnalyzer().Analyze(data);

            insights.Should().Contain(i => i.Type == FinancialInsightType.Warning && i.Message.Contains("Mercado") && i.Message.Contains("100"));
            insights.Should().Contain(i => i.Type == FinancialInsightType.Warning && i.Title == "Gastos acima da renda");
        }

        [Fact]
        public void Empty_Period_Returns_No_Insights ()
        {
            var insights = new FinancialInsightAnalyzer().Analyze(new FinancialAnalysisData());

            insights.Should().BeEmpty();
        }
    }

    public class FinancialInsightPromptBuilderTest
    {
        [Fact]
        public void Prompt_Contains_Categories_And_Goal_Progress ()
        {
            var data = InsightScenario.Build(UserBuilder.Build());

            var request = new FinancialInsightPromptBuilder().BuildFinancialInsights(data);

            var prompt = request.Messages.Last().Content;

            prompt.Should().Contain("Mercado").And.Contain("Lazer");
            prompt.Should().Contain("Metas de gastos por categoria");
            prompt.Should().Contain("120"); // 600 spent of a 500 limit => 120%
        }
    }

    public class GetFinancialInsightsUseCaseTest
    {
        [Fact]
        public async Task Success_With_AI ()
        {
            var user = UserBuilder.Build();
            var ai = new FakeAIService();

            var useCase = CreateUseCase(user , ai);

            var response = await useCase.Execute(Request() , CancellationToken.None);

            response.Insights.Should().NotBeEmpty();
            response.ArtificialInsight.Should().NotBeNull();
            response.ArtificialInsight!.Message.Should().Be(FakeAIService.DEFAULT_RESPONSE);
            ai.Requests.Should().ContainSingle();
        }

        [Fact]
        public async Task Success_When_AI_Fails_Returns_Rule_Based_Insights ()
        {
            var user = UserBuilder.Build();

            var useCase = CreateUseCase(user , new FakeAIService(new AIUnavailableException()));

            var response = await useCase.Execute(Request() , CancellationToken.None);

            response.Insights.Should().NotBeEmpty();
            response.ArtificialInsight.Should().BeNull();
        }

        [Fact]
        public async Task Error_Invalid_Period ()
        {
            var user = UserBuilder.Build();

            var useCase = CreateUseCase(user , new FakeAIService());

            var act = async () => await useCase.Execute(new RequestFinancialInsightJson { Month = 0 , Year = 0 } , CancellationToken.None);

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().HaveCount(2);
        }

        private static RequestFinancialInsightJson Request () =>
            new() { Month = InsightScenario.MONTH , Year = InsightScenario.YEAR };

        private static GetFinancialInsightsUseCase CreateUseCase ( User user , FakeAIService ai )
        {
            var data = InsightScenario.Build(user);

            var incomes = new Mock<IIncomesReadOnlyRepository>();
            incomes
                .Setup(r => r.GetByPeriodAsync(user.Id , InsightScenario.MONTH , InsightScenario.YEAR , It.IsAny<CancellationToken>()))
                .ReturnsAsync(data.Incomes);

            return new GetFinancialInsightsUseCase(
                new ExpenseReadOnlyRepositoryBuilder().GetByPeriod(user , InsightScenario.MONTH , InsightScenario.YEAR , data.Expenses.ToList()).Build() ,
                incomes.Object ,
                new FinancialGoalReadOnlyRepositoryBuilder().GetByPeriod(user , InsightScenario.MONTH , InsightScenario.YEAR , data.FinancialGoals.ToList()).Build() ,
                new FinancialInsightAnalyzer() ,
                new FinancialInsightPromptBuilder() ,
                ai ,
                LoggedUserBuilder.Build(user) ,
                new FakeRedisCacheService() ,
                NullLogger<GetFinancialInsightsUseCase>.Instance);
        }
    }
}
