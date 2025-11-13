using System.Linq;
using System.ComponentModel.DataAnnotations;
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
        return Ok(dto);
    }

    [HttpGet("filter")]
    public ActionResult<UserListDto> ListByActive([FromQuery(Name = "active")] bool isActive)
    {
        var items = userService.FilterByActive(isActive).Select(Mappers.Map).ToList();

        var dto = new UserListDto(items);
        return Ok(dto);
    }

    [HttpGet("{id:long}")]
    public ActionResult<UserListItemDto> GetById(long id)
    {
        var entity = userService.GetAll().FirstOrDefault(u => u.Id == id);
        if (entity is null)
            return NotFound();

        return Ok(entity.Map());
    }

    [HttpPost]
    public ActionResult<UserListItemDto> Create([FromBody] CreateUserRequestDto request)
    {
        // Validate using DataAnnotations on the DTO (works even when invoked directly in tests)
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            request,
            new(request),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            foreach (var vr in validationResults)
            {
                var memberName = vr.MemberNames.FirstOrDefault() ?? string.Empty;
                ModelState.AddModelError(memberName, vr.ErrorMessage ?? "Validation error");
            }
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        var user = new User
        {
            Forename = request.Forename,
            Surname = request.Surname,
            Email = request.Email,
            IsActive = request.IsActive,
            DateOfBirth = request.DateOfBirth
        };

        var dto = userService.Add(user).Map();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
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
