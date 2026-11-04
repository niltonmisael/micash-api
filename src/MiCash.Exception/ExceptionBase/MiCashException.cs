namespace MiCash.Exception.ExceptionBase
{
    public abstract class MiCashException : SystemException
    {
        protected MiCashException (string message) : base (message) { }

        public abstract int StatusCode { get; }
        public abstract List<string> GetErrors ();

        /// <summary>Machine-readable code for the app (e.g. PLAN_LIMIT_REACHED); null for most errors.</summary>
        public virtual string? ErrorCode => null;

        /// <summary>When the blocked action is allowed again (UTC); null when not applicable.</summary>
        public virtual DateTime? RetryAt => null;

    }

    
}
