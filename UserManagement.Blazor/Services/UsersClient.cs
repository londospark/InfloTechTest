using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Shared.DTOs;

namespace UserManagement.Blazor.Services;

public sealed class UsersClient : IUsersClient
{
    private readonly HttpClient _http;
    public UsersClient(HttpClient http) => _http = http;

    public async Task<UserListDto> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<UserListDto>("api/users", cancellationToken);
        return result ?? new UserListDto(Array.Empty<UserListItemDto>());
    }
}