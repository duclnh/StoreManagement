using Identity.Domain.Abstractions;
using Identity.Domain.Abstractions.Entities;
using Identity.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        public EFUnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync();
        }

        public async Task SaveChangeAsync(CancellationToken cancellation = default)
        {
            //ConvertDomainEventsToOutBoxMessages();
            UpdateAuditableEntities();
            await _dbContext.SaveChangesAsync();
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

