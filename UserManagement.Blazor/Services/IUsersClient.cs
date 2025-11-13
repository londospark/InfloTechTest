using UserManagement.Shared.DTOs;

namespace UserManagement.Blazor.Services;

public interface IUsersClient
{
    Task<UserListDto> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserListDto> GetUsersByActiveAsync(bool isActive, CancellationToken cancellationToken = default);
    Task<UserListItemDto> GetUserAsync(long id, CancellationToken cancellationToken = default);

    Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteUserAsync(long id, CancellationToken cancellationToken = default);

    Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
