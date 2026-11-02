namespace EasyPark.Platform.Shared.Domain.Repositories;

/// <summary>
///     Unit of work: commits the changes tracked by the repositories of a request.
/// </summary>
public interface IUnitOfWork
{
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Runs an operation inside a database transaction. If the operation throws, every change is rolled back.
    ///     Use it when a use case needs intermediate saves (for example to obtain a generated identifier).
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
