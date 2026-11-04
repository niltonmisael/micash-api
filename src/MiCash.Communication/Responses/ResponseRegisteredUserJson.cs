namespace MiCash.Communication.Responses
{
    public class ResponseRegisteredUserJson
    {
        public string Name { get; set; } = string.Empty;
        public string Token {  get; set; } = string.Empty;
        public string RefreshToken {  get; set; } = string.Empty;

        /// <summary>
        /// False: the app must open the "confirm your e-mail" screen; every other endpoint answers
        /// 403 EMAIL_NOT_VERIFIED until the code is typed.
        /// </summary>
        public bool EmailVerified { get; set; }
    }
}
