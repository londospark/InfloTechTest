using System.Linq;
using System.Threading.Tasks;
using UserManagement.Data.Entities;

namespace UserManagement.Services.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Return users by active state as a queryable for async enumeration
    /// </summary>
    IQueryable<User> FilterByActive(bool isActive);

    /// <summary>
    /// Retrieves all users as a queryable collection.
    /// </summary>
    /// <remarks>
    /// Returns an IQueryable that can be further filtered, sorted, or projected.
    /// The query will be executed when enumerated using async methods like ToListAsync.
    /// </remarks>
    IQueryable<User> GetAll();

    /// <summary>
    /// Add a new user asynchronously
    /// </summary>
    Task<User> AddAsync(User user);

    /// <summary>
    /// Update a user asynchronously
    /// </summary>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Delete a user by id asynchronously
    /// </summary>
    Task<bool> DeleteAsync(long id);
}
