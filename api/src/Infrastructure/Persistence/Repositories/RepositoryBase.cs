using Domain;
using Domain.Common;

namespace Infrastructure.Persistence.Repositories;

public abstract class RepositoryBase<T, TKey>(ApplicationDbContext dbContext)
    where T : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    protected ApplicationDbContext Context { get; } = dbContext;

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(entity, nameof(entity), "Empty entity received");

        try
        {
            await Context.Set<T>().AddAsync(entity, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected failure while trying to add entity of type '{typeof(T).Name}' with id '{entity.Id}'", ex);
        }
    }

    public async Task AddRangeAsync(IReadOnlyList<T> entities, bool saveChanges = true, CancellationToken cancellationToken = default)
    {
        Guard.IsTrue(entities.Count != 0, nameof(entities), "Collection cannot be empty");

        try
        {
            await Context.Set<T>().AddRangeAsync(entities, cancellationToken);
            if (saveChanges)
            {
                await Context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected failure while trying to add entities of type '{typeof(T).Name}'", ex);
        }
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(entity, nameof(entity), "Empty entity received");

        try
        {
            Context.Set<T>().Update(entity);
            await Context.SaveChangesAsync(cancellationToken);
            Context.ChangeTracker.Clear();
            return entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected failure while trying to update entity of type '{typeof(T).Name}' with id '{entity.Id}'", ex);
        }
    }

    public async Task<T> RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            Context.Set<T>().Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
            Context.ChangeTracker.Clear();
            return entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected failure while trying to delete entity of type '{typeof(T).Name}' with id '{entity.Id}'", ex);
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unexpected failure while trying to save changes", ex);
        }
    }

    public void ClearTrackedChanges()
    {
        Context.ChangeTracker.Clear();
    }
}