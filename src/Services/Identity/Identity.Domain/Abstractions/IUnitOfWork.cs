using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Abstractions
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        ///<summary>
        /// Call save change from db context
        /// </summary>
        Task SaveChangeAsync(CancellationToken cancellation = default);
    }
}

