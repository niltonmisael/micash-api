using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Services.FinancialChat;
using MiCash.Domain.Services.IA;
using System.Text;

namespace MiCash.Application.AI.PromptBuilders
{
    public class FinancialChatPromptBuilder : IFinancialChatPromptBuilder
    {
        public AIRequest Build ( FinancialChatContext context )
        {
            var request = new AIRequest();

            request.Messages.Add(CreateSystemMessage());
            request.Messages.Add(CreateFinancialContext(context));

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

                Seu objetivo é ajudar o usuário a tomar decisões financeiras inteligentes utilizando exclusivamente os dados recebidos.

                Regras:

                - Responda sempre em português do Brasil.
                - Utilize linguagem simples e amigável.
                - Seja objetiva.
                - Nunca invente receitas, despesas ou metas.
                - Utilize apenas o contexto financeiro informado.
                - Quando faltar informação, informe ao usuário quais dados seriam necessários.
                - Sempre explique o motivo das recomendações.
                - Nunca incentive endividamento irresponsável.
                - Quando identificar oportunidades de economia, destaque-as.
                - Caso existam metas financeiras, considere-as nas recomendações.
                """
            };
        }

        private static AIMessage CreateFinancialContext (
            FinancialChatContext context )
        {
            return new AIMessage
            {
                Role = AIMessageRole.User ,
                Content = BuildPrompt(context)
            };
        }

        private static string BuildPrompt (
            FinancialChatContext context )
        {
            var builder = new StringBuilder();

            AppendHeader(builder , context);

            AppendFinancialSummary(builder , context);

            AppendGoals(builder , context);

            AppendExpenseCategories(builder , context);

            AppendIncomeCategories(builder , context);

            AppendConversationHistory(
                builder ,
                context.ConversationHistory );

            AppendCurrentQuestion(
                builder ,
                context.UserMessage);

            return builder.ToString();
        }

        private static void AppendHeader (
            StringBuilder builder ,
            FinancialChatContext context )
        {
            builder.AppendLine($"Usuário: {context.UserName}");

            builder.AppendLine($"Período: {context.Month}/{context.Year}");

            builder.AppendLine();
        }

        private static void AppendFinancialSummary (
            StringBuilder builder ,
            FinancialChatContext context )
        {
            builder.AppendLine("===== RESUMO FINANCEIRO =====");

            builder.AppendLine();

            builder.AppendLine($"Receitas: R$ {context.TotalIncome:N2}");

            builder.AppendLine($"Despesas: R$ {context.TotalExpense:N2}");

            builder.AppendLine($"Saldo: R$ {context.Balance:N2}");

            builder.AppendLine();
        }

        private static void AppendGoals (
            StringBuilder builder ,
            FinancialChatContext context )
        {
            if ( !context.FinancialGoals.Any() )
                return;

            builder.AppendLine("===== METAS FINANCEIRAS =====");

            builder.AppendLine();

            foreach ( var goal in context.FinancialGoals ) {
                builder.AppendLine(
                    $"- {goal.Category.Name}: R$ {goal.TargetAmount:N2}");
            }

            builder.AppendLine();
        }

        private static void AppendExpenseCategories (
            StringBuilder builder ,
            FinancialChatContext context )
        {
            if ( !context.ExpensesByCategory.Any() )
                return;

            builder.AppendLine("===== DESPESAS POR CATEGORIA =====");

            builder.AppendLine();

            foreach ( var category in context.ExpensesByCategory ) {
                builder.AppendLine(
                    $"- {category.CategoryName}: R$ {category.Amount:N2} ({category.Percentage:N1}%)");
            }

            builder.AppendLine();
        }

        private static void AppendIncomeCategories (
            StringBuilder builder ,
            FinancialChatContext context )
        {
            if ( !context.IncomesByCategory.Any() )
                return;

            builder.AppendLine("===== RECEITAS POR CATEGORIA =====");

            builder.AppendLine();

            foreach ( var category in context.IncomesByCategory ) {
                builder.AppendLine(
                    $"- {category.CategoryName}: R$ {category.Amount:N2} ({category.Percentage:N1}%)");
            }

            builder.AppendLine();
        }

        private static void AppendConversationHistory (
            StringBuilder builder ,
            IReadOnlyList<ConversationMessage> history )
        {
            if ( !history.Any() )
                return;

            builder.AppendLine("===== HISTÓRICO DA CONVERSA =====");

            builder.AppendLine();

            foreach ( var message in history.OrderBy(x => x.CreatedAt) ) {
                var role = message.Role switch
                {
                    ConversationRole.User => "Usuário",

                    ConversationRole.Assistant => "MI",

                    ConversationRole.System => "Sistema",

                    _ => "Desconhecido"
                };

                builder.AppendLine($"{role}:");

                builder.AppendLine(message.Content);

                builder.AppendLine();
            }
        }

        private static void AppendCurrentQuestion (
            StringBuilder builder ,
            string userMessage )
        {
            builder.AppendLine("===== PERGUNTA ATUAL =====");

            builder.AppendLine();

            builder.AppendLine(userMessage);
        }
    }
}

