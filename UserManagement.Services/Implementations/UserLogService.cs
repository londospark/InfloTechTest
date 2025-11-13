using System.Collections.Generic;
using System.Linq;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;

namespace UserManagement.Services.Implementations;

public class UserLogService(IDataContext dataAccess) : IUserLogService
{
    public IEnumerable<UserLog> GetAll() => dataAccess.GetAll<UserLog>();

    public UserLog Add(UserLog log)
    {
        dataAccess.Create(log);
        return log;
    }

    public IEnumerable<UserLog> GetByUserId(long userId) =>
        dataAccess.GetAll<UserLog>().Where(l => l.UserId == userId);
}
