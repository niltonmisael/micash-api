namespace MiCash.Communication.Responses.PasswordReset
{
    /// <summary>
    /// Same answer whether or not the e-mail has an account (no user enumeration).
    /// The values are the rules of the flow, used by the app for the countdown.
    /// </summary>
    public class ResponseForgotPasswordJson
    {
        public int ResendAvailableInSeconds { get; set; }

        public int CodeExpiresInMinutes { get; set; }

        public int CodeLength { get; set; }
    }

    public class ResponsePasswordResetCodeVerifiedJson
    {
        /// <summary>Single-use authorization for /reset-password. It does not authenticate the user.</summary>
        public string ResetToken { get; set; } = string.Empty;

        public int ExpiresInMinutes { get; set; }
    }
}
