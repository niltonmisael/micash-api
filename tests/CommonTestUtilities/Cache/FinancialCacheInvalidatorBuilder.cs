using MiCash.Domain.Services.Cache.FinancialInvalidator;
using Moq;

namespace CommonTestUtilities.Cache
{
    public class FinancialCacheInvalidatorBuilder
    {
        private readonly Mock<IFinancialCacheInvalidator> _cacheInvalidator;

        public FinancialCacheInvalidatorBuilder ()
        {
            _cacheInvalidator = new Mock<IFinancialCacheInvalidator> ();
        }

        public IFinancialCacheInvalidator Build ()
        {
            return _cacheInvalidator.Object;
        }
    }
}
