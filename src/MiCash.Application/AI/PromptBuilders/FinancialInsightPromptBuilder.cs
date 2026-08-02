using MiCash.Domain.Enums;
using MiCash.Domain.Services.FinancialInsights;
using MiCash.Domain.Services.IA;
using System.Text;

namespace MiCash.Application.AI.PromptBuilders
{
    public class FinancialInsightPromptBuilder : IFinancialInsightPromptBuilder
    {
        public AIRequest BuildFinancialInsights (
            FinancialAnalysisData analysisData )
        {
            var request = new AIRequest();

            request.Messages.Add(CreateSystemMessage());

            request.Messages.Add(
                CreateFinancialContextMessage(
                    analysisData));

            return request;
        }
        private static AIMessage CreateSystemMessage ()
        {
            return new AIMessage
            {
                Role = AIMessageRole.System ,

                Content =
                """
                     Você é MI.

                     MI é a assistente financeira oficial do MiCash.

                     Seu objetivo é ajudar o usuário a melhorar sua saúde financeira.

                     Sempre:

                     - utilize linguagem simples;
                     - seja objetiva;
                     - nunca invente dados;
                     - utilize apenas os dados enviados;
                     - forneça dicas práticas;
                     - responda em português do Brasil.
                """
            };
        }

        private static AIMessage CreateFinancialContextMessage (
            FinancialAnalysisData analysisData )
        {
            var prompt = BuildPrompt(analysisData);

            return new AIMessage
            {
                Role = AIMessageRole.User ,

                Content = prompt
            };
        }

        private static string BuildPrompt (
            FinancialAnalysisData analysisData )
        {
            var builder = new StringBuilder();

            builder.AppendLine("Analise os seguintes dados:");

            builder.AppendLine();

            AppendIncome(builder , analysisData);

            AppendExpenses(builder , analysisData);

            AppendGoals(builder , analysisData);

            builder.AppendLine();

            builder.AppendLine(
                "Gere no máximo 5 insights.");

            return builder.ToString();
        }

        private static void AppendIncome (
            StringBuilder builder ,
            FinancialAnalysisData analysisData )
        {
            var totalIncome =
                analysisData.Incomes.Sum(x => x.Amount);

            builder.AppendLine(
                $"Receita total: R$ {totalIncome:N2}");
        }

        private static void AppendExpenses (
            StringBuilder builder ,
            FinancialAnalysisData analysisData )
        {
            var totalExpense =
                analysisData.Expenses.Sum(x => x.Amount);

            builder.AppendLine(
                $"Despesa total: R$ {totalExpense:N2}");
        }

        private static void AppendGoals (
            StringBuilder builder ,
            FinancialAnalysisData analysisData )
        {
            builder.AppendLine();

            builder.AppendLine("Metas:");

            foreach ( var goal in analysisData.FinancialGoals ) {
                builder.AppendLine(
                    $"{goal.Category.Name}: R$ {goal.TargetAmount:N2}");
            }
        }

    }
}
