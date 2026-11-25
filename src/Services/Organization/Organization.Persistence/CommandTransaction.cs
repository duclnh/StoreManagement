using System.Data;
using Microsoft.EntityFrameworkCore;
using Organization.Domain.Abstractions;

namespace Organization.Persistence
{
    public sealed class CommandTransaction : ICommandTransaction
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;

        public CommandTransaction(ApplicationDbContext db, IUnitOfWork unitOfWork)
        {
            _db = db;
            _unitOfWork = unitOfWork;
        }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, Func<T, bool> succeeded, CancellationToken cancellationToken)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await operation(cancellationToken);
            if (succeeded(result))
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _db.ChangeTracker.Clear();
            }

            return result;
        }
    }
}
