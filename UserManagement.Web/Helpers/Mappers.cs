using UserManagement.Data.Entities;
using UserManagement.Shared.DTOs;

namespace UserManagement.Web.Helpers;

internal static class Mappers
{
    public static UserListItemDto Map(this User user) => new(
        user.Id,
        user.Forename,
        user.Surname,
        user.Email,
        user.IsActive,
        user.DateOfBirth
    );
}
