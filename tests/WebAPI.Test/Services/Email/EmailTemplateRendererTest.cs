using FluentAssertions;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Email;
using MiCash.Infrastructure.Services.Email;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text.RegularExpressions;

namespace WebAPI.Test.Services.Email
{
    public class EmailTemplateRendererTest
    {
        private static readonly string[] Templates =
        [
            EmailTemplateRenderer.EMAIL_VERIFICATION ,
            EmailTemplateRenderer.PASSWORD_RECOVERY ,
            EmailTemplateRenderer.WELCOME ,
            EmailTemplateRenderer.PREMIUM_UPGRADE ,
            EmailTemplateRenderer.MONTHLY_SUMMARY
        ];

        private static readonly EmailTemplateOptions Links = new()
        {
            AppUrl = "https://micash.com.br/app" ,
            HelpUrl = "https://micash.com.br/ajuda" ,
            PrivacyUrl = "https://micash.com.br/privacidade" ,
            SubscriptionUrl = "https://micash.com.br/assinatura" ,
            PreferencesUrl = "https://micash.com.br/preferencias?x=1&y=2"
        };

        public static TheoryData<TransactionalEmail , string> AllEmails => new()
        {
            { new EmailVerificationEmail("Maria Souza" , "482913" , 15) , "Confirme seu e-mail · MiCash" },
            { new PasswordRecoveryEmail("Maria Souza" , "771204" , 30) , "Recuperação de senha · MiCash" },
            { new WelcomeEmail("Maria Souza") , "Bem-vindo ao MiCash" },
            { Premium() , "Seu plano Premium está ativo · MiCash" },
            { Summary() , "Seu resumo financeiro de outubro de 2026 · MiCash" }
        };

        // ---------- Templates embedded in the assembly (what "dotnet publish" ships) ----------

        [Fact]
        public void All_Five_Templates_Are_Embedded_In_Html_And_Text ()
        {
            var available = EmailTemplateEngine.AvailableTemplates();

            foreach ( var template in Templates ) {
                available.Should().Contain($"{template}.html");
                available.Should().Contain($"{template}.txt");
                EmailTemplateEngine.Load($"{template}.html").Should().StartWith("<!DOCTYPE html>");
            }

            available.Should().HaveCount(10);
        }

        [Fact]
        public void Templates_Live_In_The_Infrastructure_Dll_Not_In_Files ()
        {
            // After publish only the dll travels; there must be no dependency on a Templates folder on disk.
            var assembly = typeof(EmailTemplateEngine).Assembly;

            assembly.GetManifestResourceNames().Should().Contain("EmailTemplates.05-resumo-financeiro-mensal.html");
            Directory.Exists(Path.Combine(AppContext.BaseDirectory , "Services" , "Email" , "Templates")).Should().BeFalse();
        }

        [Fact]
        public void Unknown_Template_Is_An_Error ()
        {
            var act = () => EmailTemplateEngine.Render("99-nao-existe.html" , new EmailTemplateData());

            act.Should().Throw<InvalidOperationException>().WithMessage("*99-nao-existe.html*not found*");
        }

        [Fact]
        public void Missing_Value_Is_An_Error_Instead_Of_A_Visible_Placeholder ()
        {
            var act = () => EmailTemplateEngine.Render($"{EmailTemplateRenderer.WELCOME}.html" , new EmailTemplateData().Set("nome" , "Maria"));

            act.Should().Throw<InvalidOperationException>().WithMessage("*no value for*");
        }

        // ---------- Rendering of each e-mail ----------

        [Theory]
        [MemberData(nameof(AllEmails))]
        public void Every_Email_Renders_Without_Placeholders ( TransactionalEmail email , string subject )
        {
            var rendered = Renderer().Render(email);

            rendered.Subject.Should().Be(subject);

            foreach ( var body in new[] { rendered.HtmlBody , rendered.TextBody } ) {
                body.Should().NotContain("{{").And.NotContain("}}");
                body.Should().Contain("2026" , "the footer year comes from the Brazil clock");
            }

            rendered.HtmlBody.Should().Contain("https://micash.com.br/ajuda");
            rendered.TextBody.Should().NotContain("<");
        }

        [Fact]
        public void Verification_Shows_First_Name_Code_And_Validity ()
        {
            var rendered = Renderer().Render(new EmailVerificationEmail("  Maria Souza " , "482913" , 15));

            rendered.HtmlBody.Should().Contain("<strong>Maria</strong>")
                .And.Contain(">482913</div>")
                .And.Contain("Use o código 482913 para confirmar")
                .And.Contain("<strong>15 minutos</strong>");
            rendered.TextBody.Should().Contain("Seu código: 482913");
        }

        [Fact]
        public void Premium_Shows_Plan_Values_In_Brazilian_Format ()
        {
            var html = Decode(Renderer().Render(Premium()).HtmlBody);

            html.Should().Contain("Seu plano agora é Premium, Maria!")
                .And.Contain(">Premium Mensal</td>")
                .And.Contain("R$ 19,90 / mês")
                .And.Contain("02/11/2026")
                .And.Contain("02/12/2026")
                .And.Contain("href=\"https://micash.com.br/assinatura\"");
        }

        [Fact]
        public void Premium_Without_Renewal_Date_Shows_A_Dash ()
        {
            var html = Renderer().Render(Premium() with { NextRenewalDate = null }).HtmlBody;

            html.Should().Contain(">—</td>");
        }

        [Fact]
        public void Summary_Repeats_The_Category_Row_For_Each_Category_Up_To_Five ()
        {
            var categories = Enumerable.Range(1 , 7).Select(i => new CategorySpending($"Categoria {i}" , i * 10m)).ToList();

            var rendered = Renderer().Render(Summary() with { TopCategories = categories , TotalExpenses = 280m });
            var html = Decode(rendered.HtmlBody);

            html.Should().Contain("Onde você mais gastou");
            Regex.Matches(html , ">Categoria \\d<").Should().HaveCount(EmailTemplateRenderer.MAX_CATEGORIES);
            html.Should().Contain(">Categoria 7<").And.NotContain(">Categoria 2<" , "only the 5 largest are shown");

            // 70 of 280 = 25%; the bar width follows it.
            html.Should().Contain("R$ 70,00</strong> · 25%").And.Contain("width=\"25%\"");
            rendered.TextBody.Should().Contain("- Categoria 7: R$ 70,00 (25%)");
        }

        [Fact]
        public void Summary_Without_Categories_Hides_The_Block ()
        {
            var rendered = Renderer().Render(Summary() with { TopCategories = [] });

            rendered.HtmlBody.Should().NotContain("Onde você mais gastou").And.Contain("Mais destaques");
            rendered.TextBody.Should().NotContain("Onde você mais gastou");
        }

        [Fact]
        public void Summary_Formats_Totals_Comparison_And_Highlights ()
        {
            var html = Decode(Renderer().Render(Summary()).HtmlBody);

            html.Should().Contain("Seu mês em números, Maria")
                .And.Contain("Resumo mensal · outubro de 2026")
                .And.Contain("Comparado a setembro")
                .And.Contain(">R$ 3.200,00<")        // balance 5000 - 1800
                .And.Contain(">R$ 5.000,00<")
                .And.Contain(">R$ 1.800,00<")
                .And.Contain($"color:{EmailTemplateRenderer.COLOR_GOOD}\">+25%")   // income 4000 → 5000
                .And.Contain($"color:{EmailTemplateRenderer.COLOR_GOOD}\">-10%")   // expenses 2000 → 1800: good
                .And.Contain("R$ 950,00<div")
                .And.Contain(">Aluguel</div>")
                .And.Contain("75%<div")
                .And.Contain(">Viagem</div>")
                .And.Contain(">42<div");
        }

        [Fact]
        public void Summary_Without_Previous_Month_Or_Highlights_Shows_Neutral_Values ()
        {
            var html = Decode(Renderer().Render(Summary() with
            {
                PreviousIncome = null ,
                PreviousExpenses = 0 ,
                LargestExpense = null ,
                FocusGoal = null
            }).HtmlBody);

            html.Should().Contain($"color:{EmailTemplateRenderer.COLOR_NEUTRAL}\">—")
                .And.Contain("Nenhuma despesa")
                .And.Contain("Nenhuma meta ativa")
                .And.NotContain("—%");
        }

        // ---------- Encoding ----------

        [Fact]
        public void Html_Values_Are_Encoded_And_Text_Values_Are_Not ()
        {
            const string evil = "<script>alert(1)</script>&\"x\"";

            var rendered = Renderer().Render(Summary() with
            {
                Name = evil ,
                TopCategories = [new CategorySpending("<b>Mercado</b>" , 100m)] ,
                LargestExpense = new LargestExpense("<img src=x onerror=alert(1)>" , 10m)
            });

            rendered.HtmlBody.Should().NotContain("<script>").And.NotContain("<b>Mercado").And.NotContain("<img src=x");
            rendered.HtmlBody.Should().Contain("&lt;script&gt;").And.Contain("&lt;b&gt;Mercado");

            rendered.TextBody.Should().Contain("<b>Mercado</b>" , "plain text is not HTML and must not show entities");
        }

        [Fact]
        public void Links_Are_Encoded_Inside_Href ()
        {
            var html = Renderer().Render(Summary()).HtmlBody;

            html.Should().Contain("href=\"https://micash.com.br/preferencias?x=1&amp;y=2\"");
        }

        [Fact]
        public void A_Value_That_Looks_Like_A_Placeholder_Is_Kept_As_Text ()
        {
            var rendered = Renderer().Render(new WelcomeEmail("{{codigo}}"));

            rendered.TextBody.Should().Contain("Bem-vindo ao MiCash, {{codigo}}!");
        }

        // ---------- Invalid models ----------

        [Fact]
        public void Invalid_Models_Are_Refused ()
        {
            var renderer = Renderer();

            renderer.Invoking(r => r.Render(new EmailVerificationEmail("Maria" , " " , 15))).Should().Throw<ArgumentException>();
            renderer.Invoking(r => r.Render(new PasswordRecoveryEmail("Maria" , "123456" , 0))).Should().Throw<ArgumentException>();
            renderer.Invoking(r => r.Render(new WelcomeEmail(""))).Should().Throw<ArgumentException>();
            renderer.Invoking(r => r.Render(Summary() with { Month = 13 })).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Codes_Are_Not_Printed_By_ToString ()
        {
            new EmailVerificationEmail("Maria" , "482913" , 15).ToString().Should().NotContain("482913");
            new PasswordRecoveryEmail("Maria Souza" , "771204" , 30).ToString().Should().NotContain("771204");
        }

        [Fact]
        public void Links_Must_Be_Absolute_Https ()
        {
            var result = new EmailTemplateOptionsValidator().Validate(null , new EmailTemplateOptions
            {
                AppUrl = "https://micash.com.br" ,
                HelpUrl = "http://micash.com.br/ajuda" ,
                PrivacyUrl = "/privacidade" ,
                SubscriptionUrl = "micash://profile" ,
                PreferencesUrl = ""
            });

            result.Failures.Should().BeEquivalentTo(
                "EmailTemplates:HelpUrl must be an absolute https URL." ,
                "EmailTemplates:PrivacyUrl must be an absolute https URL." ,
                "EmailTemplates:SubscriptionUrl must be an absolute https URL." ,
                "EmailTemplates:PreferencesUrl must be an absolute https URL.");

            new EmailTemplateOptionsValidator().Validate(null , Links).Succeeded.Should().BeTrue();
        }

        private static EmailTemplateRenderer Renderer ()
        {
            var clock = new Mock<IBrazilClock>();
            clock.SetupGet(c => c.Today).Returns(new DateOnly(2026 , 11 , 2));

            return new EmailTemplateRenderer(Options.Create(Links) , clock.Object);
        }

        private static PremiumUpgradeEmail Premium () =>
            new("Maria Souza" , "Premium Mensal" , 19.90m , "mês" , new DateOnly(2026 , 11 , 2) , new DateOnly(2026 , 12 , 2));

        private static MonthlySummaryEmail Summary () => new(
            Name: "Maria Souza" ,
            Year: 2026 ,
            Month: 10 ,
            TotalIncome: 5000m ,
            TotalExpenses: 1800m ,
            PreviousIncome: 4000m ,
            PreviousExpenses: 2000m ,
            TopCategories: [new CategorySpending("Moradia" , 950m) , new CategorySpending("Mercado" , 500m)] ,
            TransactionCount: 42 ,
            LargestExpense: new LargestExpense("Aluguel" , 950m) ,
            FocusGoal: new GoalProgress("Viagem" , 75));

        // pt-BR currency uses a non-breaking space ("R$ 1,00"); normalize to compare.
        private static string Decode ( string html ) => WebUtility.HtmlDecode(html).Replace(' ' , ' ');
    }
}
