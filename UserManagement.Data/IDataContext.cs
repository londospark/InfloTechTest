using System.Linq;
using System.Threading.Tasks;

namespace UserManagement.Data;

public interface IDataContext
{
    /// <summary>
    /// Gets a queryable collection of all entities of the specified type
    /// </summary>
    /// <typeparam name="TEntity">The type of entities to retrieve</typeparam>
    /// <returns>A queryable collection of entities</returns>
    IQueryable<TEntity> GetAll<TEntity>() where TEntity : class;

    /// <summary>
    /// Creates a new entity in the data store
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to create</typeparam>
    /// <param name="entity">The entity instance to create</param>
    void Create<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Updates an existing entity in the data store
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to update</typeparam>
    /// <param name="entity">The entity instance containing updated values</param>
    void Update<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Deletes an existing entity from the data store
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to delete</typeparam>
    /// <param name="entity">The entity instance to delete</param>
    void Delete<TEntity>(TEntity entity) where TEntity : class;

    // Async counterparts -------------------------------------------------

    /// <summary>
    /// Creates a new entity in the data store asynchronously
    /// </summary>
    Task CreateAsync<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Updates an existing entity in the data store asynchronously
    /// </summary>
    Task UpdateAsync<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Deletes an existing entity from the data store asynchronously
    /// </summary>
    Task DeleteAsync<TEntity>(TEntity entity) where TEntity : class;
}
