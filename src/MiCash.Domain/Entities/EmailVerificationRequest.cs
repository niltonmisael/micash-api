using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    /// <summary>
    /// One e-mail confirmation code. Lifecycle:
    /// created (no code yet) → code sent by the e-mail job (<see cref="CodeHash"/>) → code typed in the app
    /// (<see cref="VerifiedAt"/>, the account becomes verified).
    /// A newer request, too many wrong codes, an e-mail correction or a verification set <see cref="InvalidatedAt"/>.
    /// The code is never stored in plain text.
    /// </summary>
    public class EmailVerificationRequest
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public User User { get; set; } = default!;

        public EmailVerificationPurpose Purpose { get; set; } = EmailVerificationPurpose.SignUp;

        /// <summary>
        /// Address the code is sent to. SignUp: the user's e-mail when the request was made (if the user corrects
        /// the e-mail afterwards, this request no longer proves anything). EmailChange: the new address, applied
        /// to the account only when the code is confirmed.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>BCrypt hash of the 6-digit code; null until the e-mail job generates the code.</summary>
        public string? CodeHash { get; set; }

        /// <summary>Until when the code is accepted.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Code checks made (right or wrong).</summary>
        public int Attempts { get; set; }

        public DateTime? CodeSentAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime? InvalidatedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsOpen => VerifiedAt is null && InvalidatedAt is null;
    }
}
