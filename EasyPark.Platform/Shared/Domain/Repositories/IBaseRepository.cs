namespace EasyPark.Platform.Shared.Domain.Repositories;

/// <summary>
///     Base repository contract shared by every aggregate (same pattern as the FruitLogix reference).
/// </summary>
/// <typeparam name="TEntity">The aggregate type.</typeparam>
public interface IBaseRepository<TEntity>
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}
