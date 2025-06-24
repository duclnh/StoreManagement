using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Domain.Abstractions
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        ///<summary>
        /// Call save change from db context
        /// </summary>
        Task SaveChangeAsync(CancellationToken cancellation = default);
    }
}
