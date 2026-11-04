namespace MiCash.Domain.Services.Email
{
    /// <summary>
    /// Sends a MiCash transactional e-mail: picks the template of the model, renders it and sends it.
    /// Use cases only build the model; they never see HTML.
    /// </summary>
    public interface ITransactionalEmailService
    {
        /// <param name="idempotencyKey">Optional; the same key within 24 h is not sent again (safe retries).</param>
        /// <returns>The provider's e-mail id.</returns>
        Task<string> SendAsync ( string to , TransactionalEmail email , string? idempotencyKey = null , CancellationToken cancellationToken = default );
    }

    /// <summary>Turns a <see cref="TransactionalEmail"/> into subject, HTML and text.</summary>
    public interface IEmailTemplateRenderer
    {
        RenderedEmail Render ( TransactionalEmail email );
    }

    public sealed record RenderedEmail ( string Subject , string HtmlBody , string TextBody );

    /// <summary>Base of the template models. Each derived record maps to one template.</summary>
    public abstract record TransactionalEmail;

    /// <summary>Template 01. <see cref="Code"/> is never printed by ToString (logs).</summary>
    public sealed record EmailVerificationEmail ( string Name , string Code , int ValidityMinutes ) : TransactionalEmail
    {
        public override string ToString () => $"{nameof(EmailVerificationEmail)} {{ Code = ***, ValidityMinutes = {ValidityMinutes} }}";
    }

    /// <summary>Template 02. <see cref="Code"/> is never printed by ToString.</summary>
    public sealed record PasswordRecoveryEmail ( string Name , string Code , int ValidityMinutes ) : TransactionalEmail
    {
        public override string ToString () => $"{nameof(PasswordRecoveryEmail)} {{ Code = ***, ValidityMinutes = {ValidityMinutes} }}";
    }

    /// <summary>Template 03.</summary>
    public sealed record WelcomeEmail ( string Name ) : TransactionalEmail;

    /// <summary>Template 06: code sent to the NEW address of an e-mail change. <see cref="Code"/> is never printed by ToString.</summary>
    public sealed record EmailChangeConfirmationEmail ( string Name , string Code , int ValidityMinutes ) : TransactionalEmail
    {
        public override string ToString () => $"{nameof(EmailChangeConfirmationEmail)} {{ Code = ***, ValidityMinutes = {ValidityMinutes} }}";
    }

    /// <summary>Template 07: security notice sent to the PREVIOUS address after an e-mail change.</summary>
    /// <param name="NewEmail">The new address; the template shows it masked (jo***@gmail.com).</param>
    public sealed record EmailChangedNoticeEmail ( string Name , string NewEmail ) : TransactionalEmail;

    /// <summary>Template 04.</summary>
    /// <param name="BillingPeriod">Word after the price, e.g. "mês" → "R$ 19,90 / mês".</param>
    public sealed record PremiumUpgradeEmail (
        string Name ,
        string PlanName ,
        decimal Price ,
        string BillingPeriod ,
        DateOnly StartDate ,
        DateOnly? NextRenewalDate ) : TransactionalEmail;

    /// <summary>Template 05.</summary>
    /// <param name="PreviousIncome">Income of the previous month; null when there is nothing to compare.</param>
    /// <param name="TopCategories">Expenses by category; the 5 largest are shown.</param>
    public sealed record MonthlySummaryEmail (
        string Name ,
        int Year ,
        int Month ,
        decimal TotalIncome ,
        decimal TotalExpenses ,
        decimal? PreviousIncome ,
        decimal? PreviousExpenses ,
        IReadOnlyList<CategorySpending> TopCategories ,
        int TransactionCount ,
        LargestExpense? LargestExpense ,
        GoalProgress? FocusGoal ) : TransactionalEmail;

    public sealed record CategorySpending ( string Name , decimal Amount );

    public sealed record LargestExpense ( string Description , decimal Amount );

    /// <param name="Percent">Progress from 0 to 100.</param>
    public sealed record GoalProgress ( string Name , int Percent );
}
