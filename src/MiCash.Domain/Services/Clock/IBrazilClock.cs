namespace MiCash.Domain.Services.Clock
{
    /// <summary>
    /// Calendar "now" for MiCash, a Brazil-only product: days and months are those of Brasília
    /// (America/Sao_Paulo), while instants keep being stored in UTC. In pure UTC the Brazilian
    /// day/month would flip at 21:00.
    /// </summary>
    public interface IBrazilClock
    {
        DateOnly Today { get; }

        /// <summary>Current instant in UTC.</summary>
        DateTime UtcNow { get; }

        /// <summary>The UTC instant at which <paramref name="date"/> starts in Brasília (00:00 local).</summary>
        DateTime StartOfDayUtc ( DateOnly date );
    }
}
