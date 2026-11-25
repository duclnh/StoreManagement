using Organization.Domain.Abstractions;

namespace Organization.Persistence
{
    public sealed class EFUnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;

        public EFUnitOfWork(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<int> SaveChangeAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangeAsync(cancellationToken);
    }
}
