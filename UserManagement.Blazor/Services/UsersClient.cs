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

    public async Task<UserListDto> GetUsersByActiveAsync(bool isActive, CancellationToken cancellationToken = default)
    {
        var url = $"api/users/filter?active={isActive.ToString().ToLower()}";
        var result = await http.GetFromJsonAsync<UserListDto>(url, cancellationToken);
        return result ?? new UserListDto([]);
    }

    public async Task<UserListItemDto> GetUserAsync(long id, CancellationToken cancellationToken = default)
    {
        var dto = await http.GetFromJsonAsync<UserListItemDto>($"api/users/{id}", cancellationToken);
        if (dto is null)
        {
            throw new HttpRequestException("User not found or invalid response");
        }
        return dto;
    }

    public async Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var resp = await http.PostAsJsonAsync("api/users", request, cancellationToken);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<UserListItemDto>(cancellationToken: cancellationToken);
        return dto!;
    }

    public async Task DeleteUserAsync(long id, CancellationToken cancellationToken = default)
    {
        var resp = await http.DeleteAsync($"api/users/{id}", cancellationToken);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var resp = await http.PutAsJsonAsync($"api/users/{id}", request, cancellationToken);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<UserListItemDto>(cancellationToken: cancellationToken);
        return dto!;
    }

    public async Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var res = await http.GetFromJsonAsync<PagedResultDto<UserLogDto>>($"api/users/{userId}/logs?page={page}&pageSize={pageSize}", cancellationToken);
        return res ?? new(new List<UserLogDto>(), page, pageSize, 0);
    }
}
