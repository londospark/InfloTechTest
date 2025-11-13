using System.Linq;
using System.Threading.Tasks;
using UserManagement.Data.Entities;

namespace UserManagement.Services.Interfaces;

public interface IUserLogService
{
    /// <summary>
    /// Retrieves all user logs as a queryable collection.
    /// </summary>
    IQueryable<UserLog> GetAll();

    /// <summary>
    /// Add a new user log asynchronously
    /// </summary>
    Task<UserLog> AddAsync(UserLog log);

    /// <summary>
    /// Retrieves all logs associated with a specific user id as a queryable collection.
    /// </summary>
    IQueryable<UserLog> GetByUserId(long userId);
}
