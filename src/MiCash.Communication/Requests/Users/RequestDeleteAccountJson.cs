namespace MiCash.Communication.Requests.Users
{
    /// <summary>DELETE /api/user/delete: the current password confirms that the owner is the one deleting.</summary>
    public class RequestDeleteAccountJson
    {
        public string Password { get; set; } = string.Empty;
    }
}
