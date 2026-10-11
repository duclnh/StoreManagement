using Identity.Domain.Abstractions.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Persistence.Repositories
{
    public class RepositoryBase<TEntity, TKey> : IRepositoryBase<TEntity, TKey>, IDisposable
        where TEntity : Entity<TKey>
    {
        private readonly ApplicationDbContext _dbContext;

        public RepositoryBase(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void Dispose()
        {
            _dbContext?.Dispose();
        }



        public IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>>? predicate = null, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> items = _dbContext.Set<TEntity>().AsNoTracking();

            if (includes is not null)
                foreach (var includeProperty in includes)
                    items = items.Include(includeProperty);

            if (predicate is not null)
                    items = items.Where(predicate);

            return items;
        }

        public async Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes)
        {
            return await FindAll(null,includes)
                .AsTracking()
                .SingleOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken);
        }

        public async Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes)
        {
            predicate = predicate is null ? x => true : predicate;

            return await FindAll(null, includes)
                 .AsTracking()
                 .SingleOrDefaultAsync(predicate, cancellationToken);
        }
        public void Add(TEntity entity)
        {
            _dbContext.Add(entity);
        }

        public void Remove(TEntity entity)
        {
            _dbContext.Set<TEntity>().Remove(entity);
        }

        public void RemoveMultiple(List<TEntity> entity)
        {
            _dbContext.Set<TEntity>().RemoveRange(entity);
        }

        public void Update(TEntity entity)
        {
            _dbContext.Set<TEntity>().Update(entity);
        }
    }
}

