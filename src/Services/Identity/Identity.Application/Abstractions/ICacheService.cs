using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Abstractions
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            where T : class;

        Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
            where T : class;

        Task RemoveAsync(string key, CancellationToken cancellation = default);

        Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellation = default);
    }
}

