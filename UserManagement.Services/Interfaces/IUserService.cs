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
    IEnumerable<User> GetAll();

    /// <summary>
    /// Add a new user to the data store and return the created entity
    /// </summary>
    User Add(User user);
}
