using Microsoft.Extensions.Logging;
using Moq;

namespace CommonTestUtilities.Logger
{
    public class LoggerServiceBuilder<T>
    {
        private readonly Mock<ILogger<T>> _logger;

        public LoggerServiceBuilder ()
        {
            _logger = new Mock<ILogger<T>>();
        }

        public Mock<ILogger<T>> GetMock ()
        {
            return _logger;
        }

        public ILogger<T> Build ()
        {
            return _logger.Object;
        }
    }
}