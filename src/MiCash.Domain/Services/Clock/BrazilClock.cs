namespace MiCash.Domain.Services.Clock
{
    public sealed class BrazilClock : IBrazilClock
    {
        // IANA id (works on Linux and, since .NET 6, on Windows): follows any DST law change,
        // unlike a hard-coded -03:00 offset.
        private static readonly TimeZoneInfo Brasilia =
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        private readonly TimeProvider _timeProvider;

        public BrazilClock ( TimeProvider timeProvider )
        {
            _timeProvider = timeProvider;
        }

        public DateOnly Today =>
            DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow() , Brasilia).DateTime);
    }
}
