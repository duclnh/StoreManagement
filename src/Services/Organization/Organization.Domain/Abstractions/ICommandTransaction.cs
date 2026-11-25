namespace Organization.Domain.Abstractions
{
    public interface ICommandTransaction
    {
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, Func<T, bool> succeeded, CancellationToken cancellationToken);
    }
}
