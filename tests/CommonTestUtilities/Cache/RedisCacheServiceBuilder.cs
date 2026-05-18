using MiCash.Domain.Services.Cache;
using Moq;

namespace CommonTestUtilities.Cache
{
    public class RedisCacheServiceBuilder
    {
        private readonly Mock<IRedisCacheService> _cacheService;

        public RedisCacheServiceBuilder ()
        {
            _cacheService =
                new Mock<IRedisCacheService>();
        }

        public void SetAsync ()
        {
            _cacheService
                .Setup(service => service.SetAsync(
                    It.IsAny<string>() ,
                    It.IsAny<object>() ,
                    It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);
        }

        public Mock<IRedisCacheService> GetMock ()
        {
            return _cacheService;
        }

        public IRedisCacheService Build ()
        {
            return _cacheService.Object;
        }
    }
}