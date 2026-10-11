using Identity.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Caching
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private static readonly ConcurrentDictionary<string, bool> CacheKeys = new ConcurrentDictionary<string, bool>();

        public CacheService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            string? cacheValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (cacheValue is null)
                return null;


            T? value = JsonConvert.DeserializeObject<T>(cacheValue);

            return value;
        }

        public async Task RemoveAsync(string key, CancellationToken cancellation = default)
        {
            await _distributedCache.RemoveAsync(key, cancellation);
            CacheKeys.TryRemove(key, out bool _);
        }

        public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellation = default)
        {
            //foreach (string keys in CacheKeys.Keys)
            //{
            //    if (keys.StartsWith(prefixKey))
            //    {
            //        await RemoveAsync(keys, cancellation);
            //    }
            //}
            //;

            //parallels

            IEnumerable<Task> tasks = CacheKeys.Keys.Where(x => x.StartsWith(prefixKey))
                                                    .Select(k => RemoveAsync(k, cancellation));

            await Task.WhenAll(tasks);
        }

        public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
        {
            var cacheValue = JsonConvert.SerializeObject(value);

            await _distributedCache.SetStringAsync(key, cacheValue, cancellationToken);

            CacheKeys.TryAdd(key, false);
        }
    }
}

