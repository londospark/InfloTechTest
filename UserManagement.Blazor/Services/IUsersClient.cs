using System.Threading;
using System.Threading.Tasks;
using UserManagement.Shared.DTOs;

namespace UserManagement.Blazor.Services;

public interface IUsersClient
{
    Task<UserListDto> GetUsersAsync(CancellationToken cancellationToken = default);
}