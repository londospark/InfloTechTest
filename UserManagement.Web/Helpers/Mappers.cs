using UserManagement.Data.Entities;
using UserManagement.Shared.DTOs;

namespace UserManagement.Web.Helpers;

/// <summary>
/// Mapping helpers for converting entities to DTOs used by the Web API.
/// </summary>
public static class Mappers
{
    /// <summary>
    /// Maps a <see cref="User"/> entity to <see cref="UserListItemDto"/>.
    /// </summary>
    public static UserListItemDto Map(this User user) => new(
        user.Id,
        user.Forename,
        user.Surname,
        user.Email,
        user.IsActive,
        user.DateOfBirth
    );

    /// <summary>
    /// Maps a <see cref="UserLog"/> entity to <see cref="UserLogDto"/>.
    /// </summary>
    public static UserLogDto Map(this UserLog log) => new(
        log.Id,
        log.UserId,
        log.Message,
        log.CreatedAt
    );
}
