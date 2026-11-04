namespace MiCash.API.Attributes
{
    /// <summary>
    /// The endpoint stays reachable before the e-mail is confirmed (the code screen needs it: verify, resend,
    /// correct the e-mail, profile, logout, delete the account). Everything else answers 403 EMAIL_NOT_VERIFIED.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method , Inherited = true , AllowMultiple = false)]
    public sealed class AllowUnverifiedEmailAttribute : Attribute
    {
    }
}
