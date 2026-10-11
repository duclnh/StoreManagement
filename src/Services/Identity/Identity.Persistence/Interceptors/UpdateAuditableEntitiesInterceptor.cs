using Identity.Domain.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Persistence.Interceptors
{
    public class UpdateAuditableEntitiesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
            )
        {
            DbContext? dbContext = eventData.Context;

            if (dbContext is null)
                return base.SavingChangesAsync(eventData, result, cancellationToken);

            IEnumerable<EntityEntry<IAuditTableEntity>> entityEntries =
                dbContext.ChangeTracker.Entries<IAuditTableEntity>();

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

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}

