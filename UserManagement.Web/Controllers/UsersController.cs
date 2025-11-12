using System.Linq;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.DTOs;

namespace UserManagement.Web.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public ActionResult<UserListDto> List()
    {
        var items = userService.GetAll().Select(Mappers.Map).ToList();

        var dto = new UserListDto(items);
        return dto;
    }

    [HttpGet("filter")]
    public ActionResult<UserListDto> ListByActive([FromQuery(Name = "active")] bool isActive)
    {
        var items = userService.FilterByActive(isActive).Select(Mappers.Map).ToList();

        var dto = new UserListDto(items);
        return dto;
    }

    [HttpPost]
    public ActionResult<UserListItemDto> Create([FromBody] CreateUserRequestDto request)
    {
        var user = new User
        {
            Forename = request.Forename,
            Surname = request.Surname,
            Email = request.Email,
            IsActive = request.IsActive,
            DateOfBirth = request.DateOfBirth
        };

        var created = userService.Add(user);
        var dto = Mappers.Map(created);
        return dto;
    }

}

public static class Mappers
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
