using Identity.Domain.Abstractions;
using Identity.Domain.Abstractions.Entities;
using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using Identity.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Persistence
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;

        public EFUnitOfWork(
            ApplicationDbContext dbContext,
            IRepositoryBase<LoginSession, Guid> loginSessionRepository,
            IAppUserRepository appUserRepository,
            IRolePermissionRepository rolePermissionRepository,
            IIdentityScopeRepository identityScopeRepository,
            ISecurityAuditRepository securityAuditRepository,
            IUserRoleAssignmentRepository userRoleAssignmentRepository)
        {
            _dbContext = dbContext;
            LoginSessionRepository = loginSessionRepository;
            AppUserRepository = appUserRepository;
            RolePermissionRepository = rolePermissionRepository;
            IdentityScopeRepository = identityScopeRepository;
            SecurityAuditRepository = securityAuditRepository;
            UserRoleAssignmentRepository = userRoleAssignmentRepository;
        }

        public IAppUserRepository AppUserRepository { get; }
        public IRepositoryBase<LoginSession, Guid> LoginSessionRepository { get; }
        public IRolePermissionRepository RolePermissionRepository { get; }
        public IIdentityScopeRepository IdentityScopeRepository { get; }
        public ISecurityAuditRepository SecurityAuditRepository { get; }
        public IUserRoleAssignmentRepository UserRoleAssignmentRepository { get; }

        public async Task<int> RevokeLoginSessionsAsync(Guid userId, Guid? sessionId = null, Guid? exceptSessionId = null,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var query = _dbContext.LoginSessions.Where(item => item.UserId == userId && !item.IsRevoked
                && (!sessionId.HasValue || item.Id == sessionId.Value)
                && (!exceptSessionId.HasValue || item.Id != exceptSessionId.Value));
            // Atomic column updates win over concurrent refreshes without writing a stale token hash.
            if (_dbContext.Database.IsRelational())
                return await query.ExecuteUpdateAsync(update => update.SetProperty(item => item.IsRevoked, true)
                    .SetProperty(item => item.RevokedAtUtc, now), cancellationToken);
            var sessions = await query.ToListAsync(cancellationToken);
            foreach (var session in sessions) session.Revoke(now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return sessions.Count;
        }

        public async Task ExecuteInSerializableTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync();
        }

        public async Task SaveChangeAsync(CancellationToken cancellation = default)
        {
            //ConvertDomainEventsToOutBoxMessages();
            UpdateAuditableEntities();
            await _dbContext.SaveChangesAsync(cancellation);
        }

        private void UpdateAuditableEntities()
        {
            IEnumerable<EntityEntry<IAuditTableEntity>> entityEntries =
                  _dbContext.ChangeTracker.Entries<IAuditTableEntity>();

            foreach (EntityEntry<IAuditTableEntity> entityEntry in entityEntries)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    entityEntry.Property(x => x.CreatedOnUtc).CurrentValue = DateTime.UtcNow;
                }

                if (entityEntry.State == EntityState.Modified)
                {
                    entityEntry.Property(x => x.UpdatedOnUtc).CurrentValue = DateTime.UtcNow;
                }
            }
        }

        private void ConvertDomainEventsToOutBoxMessages()
        {
            var outboxMessages = _dbContext.ChangeTracker
                .Entries<Domain.Abstractions.Aggregates.AggregateRoot<Guid>>()
                .Select(x => x.Entity)
                .SelectMany(aggregateRoot =>
                {
                    var domainEvents = aggregateRoot.GetDomainEvents();

                    aggregateRoot.ClearDomainEvents();
                    return domainEvents;
                }).Select(domainEvent => new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    OccurredOnUtc = DateTime.UtcNow,
                    Type = domainEvent.GetType().Name,
                    Content = JsonConvert.SerializeObject(domainEvent,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    })
                }).ToList();

            _dbContext.Set<OutboxMessage>().AddRange(outboxMessages);
        }
    }
}
