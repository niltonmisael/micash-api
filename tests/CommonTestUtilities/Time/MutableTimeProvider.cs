namespace CommonTestUtilities.Time
{
    /// <summary>TimeProvider whose "now" the test controls.</summary>
    public sealed class MutableTimeProvider ( DateTimeOffset now ) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public DateTime UtcNow => Now.UtcDateTime;

        public override DateTimeOffset GetUtcNow () => Now;

        public void Advance ( TimeSpan by ) => Now += by;
    }
}
