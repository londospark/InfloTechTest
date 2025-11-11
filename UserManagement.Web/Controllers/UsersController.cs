using System.Linq;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.Users;
using UserManagement.Shared.DTOs;

namespace UserManagement.WebMS.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public ActionResult<UserListDto> List()
    {
        var items = userService.GetAll().Select(p => new UserListItemDto(
            p.Id,
            p.Forename,
            p.Surname,
            p.Email,
            p.IsActive
        )).ToList();

        var dto = new UserListDto(items);
        return dto;
    }
}
