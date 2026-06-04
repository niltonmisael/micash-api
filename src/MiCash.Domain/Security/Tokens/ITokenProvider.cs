namespace MiCash.Domain.Security.Tokens
{
    public interface ITokenProvider
    {
        string TokenOnRequest ();
    }
}
