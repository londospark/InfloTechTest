using System.Linq;
using System.Threading.Tasks;
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
    public IQueryable<User> FilterByActive(bool isActive) =>
        dataAccess.GetAll<User>().Where(u => u.IsActive == isActive);

    public IQueryable<User> GetAll() => dataAccess.GetAll<User>();

    public async Task<User> AddAsync(User user)
    {
        await dataAccess.CreateAsync(user);
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        await dataAccess.UpdateAsync(user);
        return user;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var entity = dataAccess.GetAll<User>().FirstOrDefault(u => u.Id == id);
        if (entity is null)
            return false;
        await dataAccess.DeleteAsync(entity);
        return true;
    }
}
