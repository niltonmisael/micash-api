namespace MiCash.Domain.Entities
{
    public class User
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public Guid UserIdentifier { get; set; } = Guid.NewGuid();

        /// <summary>
        /// When the user typed the code sent to <see cref="Email"/> (or proved the address with a password reset).
        /// Null: the account exists, but the API answers 403 EMAIL_NOT_VERIFIED to everything except the
        /// verification endpoints until the address is confirmed.
        /// </summary>
        public DateTime? EmailVerifiedAt { get; set; }
        /// <summary>Monthly summary (template 05) by e-mail. On by default: it is about the user's own data.</summary>
        public bool MonthlySummaryEmail { get; set; } = true;
        /// <summary>Short push "Seu resumo de outubro chegou". On by default.</summary>
        public bool MonthlySummaryPush { get; set; } = true;
        /// <summary>News and offers by e-mail (marketing): off until the user opts in (LGPD).</summary>
        public bool NewsEmail { get; set; }
        /// <summary>First day of the last month whose summary was sent; keeps the monthly job idempotent.</summary>
        public DateOnly? MonthlySummarySentFor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Category> Categories { get; set; } = [];

        public ICollection<Expense> Expenses { get; set; } = [];

        public ICollection<Income> Incomes { get; set; } = [];

        public ICollection<Subscription> Subscriptions { get; set; } = [];

        /// <summary>Everyone who paid (or pays) this user's subscriptions; the user may be one of them.</summary>
        public ICollection<PaymentCustomer> PaymentCustomers { get; set; } = [];

        public ICollection<FinancialGoal> FinancialGoals { get; set; } = [];

        public ICollection<Conversation> Conversations { get; set; } = [];

    }
}
