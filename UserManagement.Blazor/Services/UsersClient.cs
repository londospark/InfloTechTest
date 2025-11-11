using System.Net.Http.Json;
using UserManagement.Shared.DTOs;

namespace UserManagement.Blazor.Services;

public sealed class UsersClient(HttpClient http) : IUsersClient
{
    public async Task<UserListDto> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var result = await http.GetFromJsonAsync<UserListDto>("api/users", cancellationToken);
        return result ?? new UserListDto([]);
    }
}