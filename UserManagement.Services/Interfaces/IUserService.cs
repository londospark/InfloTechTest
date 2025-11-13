using System.Collections.Generic;
using UserManagement.Data.Entities;

namespace UserManagement.Services.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Return users by active state
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    IEnumerable<User> FilterByActive(bool isActive);

    /// <summary>
    /// Retrieves all users from the underlying data store.
    /// </summary>
    /// <remarks>
    /// The returned sequence is typically backed by the data provider (e.g., EF Core)
    /// and will be evaluated on enumeration. Consumers may further filter or project
    /// the results; however, prefer using dedicated service methods when applying
    /// domain-specific rules.
    /// </remarks>
    /// <returns>
    /// An enumerable sequence of all <see cref="User"/> entities available in the data store.
    /// </returns>
    IEnumerable<User> GetAll();

    /// <summary>
    /// Add a new user to the data store and return the created entity
    /// </summary>
    User Add(User user);

    /// <summary>
    /// Delete a user by id.
    /// Returns true when found and deleted; false when not found.
    /// </summary>
    bool Delete(long id);
}
