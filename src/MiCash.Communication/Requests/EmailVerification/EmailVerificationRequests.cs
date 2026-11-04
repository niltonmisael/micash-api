namespace MiCash.Communication.Requests.EmailVerification
{
    public class RequestVerifyEmailJson
    {
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>Fixes a mistyped address before it is confirmed (a new code goes to the new address).</summary>
    public class RequestChangeVerificationEmailJson
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Step 1 of the e-mail change of a verified account: a code goes to <see cref="NewEmail"/>; the account keeps
    /// the current e-mail until the code is confirmed. Calling it again (after 60s) resends the code.
    /// </summary>
    public class RequestEmailChangeJson
    {
        public string NewEmail { get; set; } = string.Empty;

        /// <summary>Current password: a stolen session alone cannot move the account to another mailbox.</summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Step 2: the code received at the new address.</summary>
    public class RequestConfirmEmailChangeJson
    {
        public string Code { get; set; } = string.Empty;
    }
}
