namespace MiCash.Domain.Security.Tokens
{
    public interface IRefreshTokenGenerator
    {
        string Generate ();
    }
}
