using MiCash.Domain.Services.Cache;

namespace CommonTestUtilities.Cache
{
    public class FakeRedisCacheService : IRedisCacheService
    {
        private readonly Dictionary<string , object> _cache = [];

        public Task<T?> GetAsync<T> ( string key )
        {
            if ( _cache.TryGetValue(key , out var value) ) {
                return Task.FromResult(( T? ) value);
            }

            return Task.FromResult(default(T));
        }

        public Task RemoveAsync ( string key )
        {
            _cache.Remove(key);

            return Task.CompletedTask;
        }

        public Task SetAsync<T> ( 
            string key , 
            T value , 
            TimeSpan? expiration = null )
        {
            _cache[key] = value;

            return Task.CompletedTask;
        }
    }
}