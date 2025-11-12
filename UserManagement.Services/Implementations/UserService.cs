using System.Collections.Generic;
using System.Linq;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;

namespace UserManagement.Services.Implementations;

public class UserService(IDataContext dataAccess) : IUserService
{
    /// <summary>
    /// Return users by active state
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public IEnumerable<User> FilterByActive(bool isActive) =>
        dataAccess.GetAll<User>().Where(u => u.IsActive == isActive);

    public IEnumerable<User> GetAll() => dataAccess.GetAll<User>();

    public User Add(User user)
    {
        dataAccess.Create(user);
        return user;
    }
}
