using System.Linq;
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
