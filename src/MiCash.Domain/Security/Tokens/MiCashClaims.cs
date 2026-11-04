namespace MiCash.Domain.Security.Tokens
{
    /// <summary>Custom claims of the MiCash access token.</summary>
    public static class MiCashClaims
    {
        /// <summary>
        /// "true" or "false". Tokens issued before e-mail verification existed do not carry it: those users
        /// were all marked as verified by the migration, so a missing claim counts as verified.
        /// </summary>
        public const string EMAIL_VERIFIED = "email_verified";
    }
}
