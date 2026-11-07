using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;

namespace Identity.Domain.Abstractions
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IAppUserRepository AppUserRepository { get; }
        IRepositoryBase<LoginSession, Guid> LoginSessionRepository { get; }
        IRolePermissionRepository RolePermissionRepository { get; }
        ISecurityAuditRepository SecurityAuditRepository { get; }
        IUserRoleAssignmentRepository UserRoleAssignmentRepository { get; }

        ///<summary>
        /// Call save change from db context
        /// </summary>
        Task SaveChangeAsync(CancellationToken cancellation = default);
    }
}
