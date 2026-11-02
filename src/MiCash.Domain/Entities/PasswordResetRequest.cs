namespace MiCash.Domain.Entities
{
    /// <summary>
    /// One "forgot my password" request. Lifecycle:
    /// created (no code yet) → code sent by the e-mail job (<see cref="CodeHash"/>) → code verified
    /// (<see cref="VerifiedAt"/>, issues the single-use reset token) → password changed (<see cref="CompletedAt"/>).
    /// A newer request, too many wrong codes or a completed reset set <see cref="InvalidatedAt"/>.
    /// Neither the code nor the reset token is stored in plain text.
    /// </summary>
    public class PasswordResetRequest
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public User User { get; set; } = default!;

        /// <summary>BCrypt hash of the 6-digit code; null until the e-mail job generates the code.</summary>
        public string? CodeHash { get; set; }

        /// <summary>Until when the code is accepted.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Code checks made (right or wrong).</summary>
        public int Attempts { get; set; }

        public DateTime? CodeSentAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        /// <summary>SHA-256 (hex) of the reset token handed out after the code is verified.</summary>
        public string? ResetTokenHash { get; set; }

        public DateTime? ResetTokenExpiresAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? InvalidatedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsOpen => CompletedAt is null && InvalidatedAt is null;
    }
}
