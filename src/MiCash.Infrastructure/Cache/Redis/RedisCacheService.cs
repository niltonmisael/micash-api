using MiCash.Domain.Services.Cache;
using StackExchange.Redis;
using System.Text.Json;

namespace MiCash.Infrastructure.Cache.Redis
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService ( IConnectionMultiplexer redis )
        {
            _database = redis.GetDatabase();
        }

        public async Task SetAsync<T> (
            string key ,
            T value ,
            TimeSpan? expiration = null )
        {
            var json = JsonSerializer.Serialize(value);

            await _database.StringSetAsync(
                key: key ,
                value: json ,
                expiry: expiration.HasValue ? expiration.Value : default
            );
        }

        public async Task<T?> GetAsync<T> ( string key )
        {
            var value = await _database.StringGetAsync(key);

            if ( value.IsNullOrEmpty )
                return default;

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task RemoveAsync ( string key )
        {
            await _database.KeyDeleteAsync(key);
        }
    }
}
