using System.Net;

namespace MiCash.Exception.ExceptionBase
{
    /// <summary>
    /// The profile update tried to change the e-mail directly. 400 with errorCode EMAIL_CHANGE_REQUIRES_CONFIRMATION:
    /// the app opens the e-mail change flow (POST api/user/email-change → code to the new address → confirm).
    /// </summary>
    public class EmailChangeRequiresConfirmationException : MiCashException
    {
        public const string CODE = "EMAIL_CHANGE_REQUIRES_CONFIRMATION";

        public EmailChangeRequiresConfirmationException () : base(ResourceErrorMessages.EMAIL_CHANGE_REQUIRES_CONFIRMATION) { }

        public override int StatusCode => ( int ) HttpStatusCode.BadRequest;

        public override string? ErrorCode => CODE;

        public override List<string> GetErrors () => [Message];
    }
}
