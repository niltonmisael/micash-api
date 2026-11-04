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

        public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public DateTime StartOfDayUtc ( DateOnly date ) => StartOfBrasiliaDayUtc(date);

        /// <summary>Midnight of the date in Brasília, as a UTC instant.</summary>
        public static DateTime StartOfBrasiliaDayUtc ( DateOnly date ) =>
            TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue , DateTimeKind.Unspecified) , Brasilia);

        /// <summary>
        /// UTC bounds [start, end) of a calendar month in Brasília, for filtering UTC instants such as
        /// Expense.ExpenseDate: an expense at 22:30 on the last day belongs to that month, not the next.
        /// </summary>
        public static (DateTime StartUtc, DateTime EndUtc) MonthRangeUtc ( int year , int month )
        {
            var first = new DateOnly(year , month , 1);

            return (StartOfBrasiliaDayUtc(first), StartOfBrasiliaDayUtc(first.AddMonths(1)));
        }

        /// <summary>A stored UTC instant as Brasília wall time (its calendar day and month). Unspecified kind is UTC.</summary>
        public static DateTime ToBrasilia ( DateTime utc )
        {
            var asUtc = utc.Kind switch {
                DateTimeKind.Utc => utc ,
                DateTimeKind.Local => utc.ToUniversalTime() ,
                _ => DateTime.SpecifyKind(utc , DateTimeKind.Utc)
            };

            return TimeZoneInfo.ConvertTimeFromUtc(asUtc , Brasilia);
        }
    }
}
