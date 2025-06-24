using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Domain.Repositories
{
    public interface IRepositoryBaseDbContext<TContext, TEntity, in TKey> 
        where TContext : DbContext
        where TEntity : class
    {
        Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);
        Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);
        IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>>? predicate = null, params Expression<Func<TEntity, object>>[] includes);
        void Add(TEntity entity);
        void Remove(TEntity entity);
        void RemoveMutiple(List<TEntity> entity);
        void Update(TEntity entity);
    }
}
