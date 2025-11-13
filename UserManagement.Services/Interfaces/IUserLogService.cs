using System.Collections.Generic;
using UserManagement.Data.Entities;

namespace UserManagement.Services.Interfaces;

public interface IUserLogService
{
    /// <summary>
    /// Retrieves all user logs from the data store.
    /// </summary>
    IEnumerable<UserLog> GetAll();

    /// <summary>
    /// Add a new user log to the data store and return the created entity.
    /// </summary>
    UserLog Add(UserLog log);

    /// <summary>
    /// Retrieves all logs associated with a specific user id.
    /// </summary>
    IEnumerable<UserLog> GetByUserId(long userId);
}
