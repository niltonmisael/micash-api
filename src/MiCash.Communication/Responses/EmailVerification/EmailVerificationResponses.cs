namespace MiCash.Communication.Responses.EmailVerification
{
    /// <summary>A code was (or will be, by the e-mail job) sent. The values drive the app's countdown.</summary>
    public class ResponseEmailVerificationCodeSentJson
    {
        /// <summary>Where the code goes (the account e-mail).</summary>
        public string Email { get; set; } = string.Empty;

        public int ResendAvailableInSeconds { get; set; }

        public int CodeExpiresInMinutes { get; set; }

        public int CodeLength { get; set; }
    }

    /// <summary>
    /// E-mail confirmed: new session tokens (the previous access token still says "not verified")
    /// and whether the app should show the Premium offer before the home screen.
    /// </summary>
    public class ResponseEmailVerifiedJson : ResponseRegisteredUserJson
    {
        /// <summary>
        /// True when the user is not Premium yet (the usual case right after sign-up). For the WEB sign-up only:
        /// subscriptions are sold on the web, the mobile apps are consumption-only and must ignore this flag
        /// (no offer, no link to a checkout).
        /// </summary>
        public bool ShowPremiumOffer { get; set; }
    }
}
