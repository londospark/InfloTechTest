using System.Linq;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Services.Implementations;

public class UserLogService(IDataContext dataAccess) : IUserLogService
{
    public IQueryable<UserLog> GetAll() => dataAccess.GetAll<UserLog>();

    public async Task<UserLog> AddAsync(UserLog log)
    {
        await dataAccess.CreateAsync(log);
        return log;
    }

    public IQueryable<UserLog> GetByUserId(long userId) =>
        dataAccess.GetAll<UserLog>().Where(l => l.UserId == userId);
}
