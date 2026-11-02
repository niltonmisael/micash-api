namespace MiCash.Communication.Requests.PasswordReset
{
    public class RequestForgotPasswordJson
    {
        public string Email { get; set; } = string.Empty;
    }

    public class RequestVerifyPasswordResetCodeJson
    {
        public string Email { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }

    public class RequestResetPasswordJson
    {
        /// <summary>Single-use authorization returned by /verify-password-reset-code (not a login token).</summary>
        public string ResetToken { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}
