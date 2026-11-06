using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Email;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace MiCash.Infrastructure.Services.Email
{
    /// <summary>Section "EmailTemplates": absolute https links used by the templates' buttons and footers.</summary>
    public sealed class EmailTemplateOptions
    {
        public const string SECTION = "EmailTemplates";

        /// <summary>"Começar a organizar", "Aproveitar o Premium", "Ver relatório completo".</summary>
        public string AppUrl { get; set; } = string.Empty;

        public string HelpUrl { get; set; } = string.Empty;

        public string PrivacyUrl { get; set; } = string.Empty;

        /// <summary>"Perfil → Assinatura" (template 04).</summary>
        public string SubscriptionUrl { get; set; } = string.Empty;

        /// <summary>"Gerenciar e-mails" (template 05).</summary>
        public string PreferencesUrl { get; set; } = string.Empty;
    }

    public sealed class EmailTemplateOptionsValidator : IValidateOptions<EmailTemplateOptions>
    {
        public ValidateOptionsResult Validate ( string? name , EmailTemplateOptions options )
        {
            var failures = new (string Key, string Value)[]
            {
                (nameof(options.AppUrl) , options.AppUrl) ,
                (nameof(options.HelpUrl) , options.HelpUrl) ,
                (nameof(options.PrivacyUrl) , options.PrivacyUrl) ,
                (nameof(options.SubscriptionUrl) , options.SubscriptionUrl) ,
                (nameof(options.PreferencesUrl) , options.PreferencesUrl)
            }
            .Where(x => Uri.TryCreate(x.Value , UriKind.Absolute , out var uri) == false || uri.Scheme != Uri.UriSchemeHttps)
            .Select(x => $"{EmailTemplateOptions.SECTION}:{x.Key} must be an absolute https URL.")
            .ToList();

            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }
    }

    /// <summary>
    /// Maps each <see cref="TransactionalEmail"/> to its template (Services/Email/Templates) and formats the values
    /// in pt-BR (R$, dates, month names). The subject is the template's &lt;title&gt;.
    /// </summary>
    public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        public const string EMAIL_VERIFICATION = "01-verificacao-de-email";
        public const string PASSWORD_RECOVERY = "02-recuperacao-de-senha";
        public const string WELCOME = "03-boas-vindas";
        public const string PREMIUM_UPGRADE = "04-upgrade-premium";
        public const string MONTHLY_SUMMARY = "05-resumo-financeiro-mensal";
        public const string EMAIL_CHANGE_CONFIRMATION = "06-confirmacao-novo-email";
        public const string EMAIL_CHANGED_NOTICE = "07-aviso-email-alterado";
        public const string ACCOUNT_DELETED = "08-conta-excluida";

        public const int MAX_CATEGORIES = 5;

        // Colors of template 05 (only constants go into style="").
        public const string COLOR_GOOD = "#4E9E6B";
        public const string COLOR_BAD = "#C86A52";
        public const string COLOR_NEUTRAL = "#8A8A8A";

        private const string EMPTY = "—";

        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private static readonly Regex TitlePattern = new("<title>(?<title>.*?)</title>" , RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private readonly EmailTemplateOptions _options;
        private readonly IBrazilClock _clock;

        public EmailTemplateRenderer ( IOptions<EmailTemplateOptions> options , IBrazilClock clock )
        {
            _options = options.Value;
            _clock = clock;
        }

        public RenderedEmail Render ( TransactionalEmail email )
        {
            ArgumentNullException.ThrowIfNull(email);

            var (template, data) = email switch
            {
                EmailVerificationEmail e => (EMAIL_VERIFICATION , EmailVerification(e)),
                PasswordRecoveryEmail e => (PASSWORD_RECOVERY , PasswordRecovery(e)),
                WelcomeEmail e => (WELCOME , new EmailTemplateData().Set("nome" , FirstName(e.Name))),
                PremiumUpgradeEmail e => (PREMIUM_UPGRADE , PremiumUpgrade(e)),
                MonthlySummaryEmail e => (MONTHLY_SUMMARY , MonthlySummary(e)),
                EmailChangeConfirmationEmail e => (EMAIL_CHANGE_CONFIRMATION , EmailChangeConfirmation(e)),
                EmailChangedNoticeEmail e => (EMAIL_CHANGED_NOTICE , new EmailTemplateData()
                    .Set("nome" , FirstName(e.Name))
                    .Set("novo_email" , MaskEmail(e.NewEmail))),
                AccountDeletedEmail e => (ACCOUNT_DELETED , new EmailTemplateData().Set("nome" , FirstName(e.Name))),
                _ => throw new ArgumentException($"No e-mail template for {email.GetType().Name}." , nameof(email))
            };

            data.Set("ano" , _clock.Today.Year.ToString(CultureInfo.InvariantCulture))
                .Set("url_app" , _options.AppUrl)
                .Set("url_ajuda" , _options.HelpUrl)
                .Set("url_privacidade" , _options.PrivacyUrl)
                .Set("url_assinatura" , _options.SubscriptionUrl)
                .Set("url_preferencias" , _options.PreferencesUrl);

            var html = EmailTemplateEngine.Render($"{template}.html" , data);
            var text = EmailTemplateEngine.Render($"{template}.txt" , data);

            var subject = WebUtility.HtmlDecode(TitlePattern.Match(html).Groups["title"].Value).Trim();

            return new RenderedEmail(subject , html , text);
        }

        private static EmailTemplateData EmailVerification ( EmailVerificationEmail e ) =>
            new EmailTemplateData()
                .Set("nome" , FirstName(e.Name))
                .Set("codigo" , Required(e.Code , nameof(e.Code)))
                .Set("validade_minutos" , Minutes(e.ValidityMinutes));

        private static EmailTemplateData EmailChangeConfirmation ( EmailChangeConfirmationEmail e ) =>
            new EmailTemplateData()
                .Set("nome" , FirstName(e.Name))
                .Set("codigo" , Required(e.Code , nameof(e.Code)))
                .Set("validade_minutos" , Minutes(e.ValidityMinutes));

        /// <summary>
        /// "joana.lima@gmail.com" → "jo***@gmail.com": enough for the owner to recognize it, without handing the
        /// full new address to whoever reads the old mailbox.
        /// </summary>
        public static string MaskEmail ( string email )
        {
            var value = Required(email , nameof(email));
            var at = value.LastIndexOf('@');

            if ( at <= 0 ) {
                return "***";
            }

            var local = value[..at];
            var visible = local.Length <= 2 ? local[..1] : local[..2];

            return $"{visible}***{value[at..]}";
        }

        private static EmailTemplateData PasswordRecovery ( PasswordRecoveryEmail e ) =>
            new EmailTemplateData()
                .Set("nome" , FirstName(e.Name))
                .Set("codigo" , Required(e.Code , nameof(e.Code)))
                .Set("validade_minutos" , Minutes(e.ValidityMinutes));

        private static EmailTemplateData PremiumUpgrade ( PremiumUpgradeEmail e ) =>
            new EmailTemplateData()
                .Set("nome" , FirstName(e.Name))
                .Set("plano_nome" , Required(e.PlanName , nameof(e.PlanName)))
                .Set("plano_valor" , Money(e.Price))
                .Set("plano_periodicidade" , Required(e.BillingPeriod , nameof(e.BillingPeriod)))
                .Set("data_inicio" , Date(e.StartDate))
                .Set("data_renovacao" , e.NextRenewalDate is { } renewal ? Date(renewal) : EMPTY);

        private static EmailTemplateData MonthlySummary ( MonthlySummaryEmail e )
        {
            if ( e.Month is < 1 or > 12 )
                throw new ArgumentOutOfRangeException(nameof(e.Month) , "Month must be between 1 and 12.");

            var month = new DateOnly(e.Year , e.Month , 1);
            var previous = month.AddMonths(-1);

            var (incomeChange, incomeColor) = Variation(e.TotalIncome , e.PreviousIncome , higherIsBetter: true);
            var (expenseChange, expenseColor) = Variation(e.TotalExpenses , e.PreviousExpenses , higherIsBetter: false);

            var categories = (e.TopCategories ?? [])
                .Where(c => c.Amount > 0)
                .OrderByDescending(c => c.Amount)
                .Take(MAX_CATEGORIES)
                .Select(c => {
                    var percent = e.TotalExpenses > 0
                        ? (int)Math.Round(c.Amount / e.TotalExpenses * 100 , MidpointRounding.AwayFromZero)
                        : 0;

                    return new EmailTemplateData()
                        .Set("cat_nome" , Required(c.Name , nameof(c.Name)))
                        .Set("cat_valor" , Money(c.Amount))
                        .Set("cat_percentual" , percent.ToString(CultureInfo.InvariantCulture))
                        // A 0% cell collapses badly in some clients: the bar shows at least 1%.
                        .Set("cat_barra" , Math.Clamp(percent , 1 , 100).ToString(CultureInfo.InvariantCulture));
                })
                .ToList();

            return new EmailTemplateData()
                .Set("nome" , FirstName(e.Name))
                .Set("mes_referencia" , month.ToString("MMMM 'de' yyyy" , PtBr))
                .Set("mes_anterior" , previous.ToString("MMMM" , PtBr))
                .Set("saldo_mensal" , Money(e.TotalIncome - e.TotalExpenses))
                .Set("total_receitas" , Money(e.TotalIncome))
                .Set("total_despesas" , Money(e.TotalExpenses))
                .Set("receitas_variacao" , incomeChange)
                .Set("receitas_variacao_cor" , incomeColor)
                .Set("despesas_variacao" , expenseChange)
                .Set("despesas_variacao_cor" , expenseColor)
                .Show("categorias_bloco" , categories.Count > 0)
                .Section("categorias" , categories)
                .Set("maior_despesa_valor" , e.LargestExpense is { } largest ? Money(largest.Amount) : EMPTY)
                .Set("maior_despesa_descricao" , e.LargestExpense?.Description is { Length: > 0 } description ? description : "Nenhuma despesa")
                .Set("qtd_transacoes" , e.TransactionCount.ToString("N0" , PtBr))
                .Set("meta_percentual" , e.FocusGoal is { } goal ? $"{Math.Clamp(goal.Percent , 0 , 100)}%" : EMPTY)
                .Set("meta_nome" , e.FocusGoal?.Name is { Length: > 0 } goalName ? goalName : "Nenhuma meta ativa");
        }

        /// <summary>"+12,5%" / "-8%" with the color meaning good or bad; "—" when there is nothing to compare.</summary>
        private static (string Text, string Color) Variation ( decimal current , decimal? previous , bool higherIsBetter )
        {
            if ( previous is null or 0 )
                return (EMPTY , COLOR_NEUTRAL);

            var change = Math.Round((current - previous.Value) / Math.Abs(previous.Value) * 100 , 1 , MidpointRounding.AwayFromZero);

            if ( change == 0 )
                return ("0%" , COLOR_NEUTRAL);

            var text = (change > 0 ? "+" : "-") + Math.Abs(change).ToString("0.#" , PtBr) + "%";
            var isGood = change > 0 == higherIsBetter;

            return (text , isGood ? COLOR_GOOD : COLOR_BAD);
        }

        private static string FirstName ( string name ) =>
            Required(name , nameof(name)).Split(' ' , StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];

        private static string Money ( decimal value ) => value.ToString("C2" , PtBr);

        private static string Date ( DateOnly value ) => value.ToString("dd/MM/yyyy" , PtBr);

        private static string Minutes ( int minutes ) =>
            minutes > 0
                ? minutes.ToString(CultureInfo.InvariantCulture)
                : throw new ArgumentOutOfRangeException(nameof(minutes) , "Validity must be greater than zero.");

        private static string Required ( string? value , string name ) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException($"{name} is required." , name)
                : value.Trim();
    }
}
