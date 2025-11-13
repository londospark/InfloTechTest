using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.DTOs;
using UserManagement.Web.Helpers;

namespace UserManagement.Web.Controllers;

/// <summary>
/// Provides RESTful endpoints for querying and managing users.
/// </summary>
/// <remarks>
/// All endpoints are rooted at <c>/api/users</c> and return JSON payloads.
/// The controller leverages data annotations on DTOs for request validation, and returns
/// standard HTTP status codes suitable for API clients and documentation tools like Scalar.
/// </remarks>
[ApiController]
[Route("api/users")]
[Tags("Users")]
[Produces("application/json")]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Retrieves the full list of users.
    /// </summary>
    /// <returns>
    /// 200 OK with a <see cref="UserListDto"/> containing all users.
    /// </returns>
    /// <response code="200">Returns the list of users.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserListDto), StatusCodes.Status200OK)]
    public ActionResult<UserListDto> List()
    {
        var items = userService.GetAll().Select(Mappers.Map).ToList();

        var dto = new UserListDto(items);
        return Ok(dto);
    }

    /// <summary>
    /// Retrieves users filtered by their active state.
    /// </summary>
    /// <param name="isActive">When true returns only active users; when false returns only inactive users.</param>
    /// <remarks>
    /// Example: <c>GET /api/users/filter?active=true</c>
    /// </remarks>
    /// <returns>200 OK with a filtered <see cref="UserListDto"/>.</returns>
    /// <response code="200">Returns the filtered list of users.</response>
    [HttpGet("filter")]
    [ProducesResponseType(typeof(UserListDto), StatusCodes.Status200OK)]
    public ActionResult<UserListDto> ListByActive([FromQuery(Name = "active")] bool isActive)
    {
        var items = userService.FilterByActive(isActive).Select(Mappers.Map).ToList();

        var dto = new UserListDto(items);
        return Ok(dto);
    }

    /// <summary>
    /// Retrieves a single user by their identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>200 OK with the user, or 404 Not Found if the user does not exist.</returns>
    /// <response code="200">Returns the user.</response>
    /// <response code="404">No user with the specified ID was found.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserListItemDto> GetById(long id)
    {
        var entity = userService.GetAll().FirstOrDefault(u => u.Id == id);
        if (entity is null)
            return NotFound();

        return Ok(entity.Map());
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">The details for the new user. Properties are validated using data annotations.</param>
    /// <returns>201 Created with the created user in the body and a Location header referencing the resource.</returns>
    /// <response code="201">The user was created successfully.</response>
    /// <response code="400">The request payload failed validation. See response body for details.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<UserListItemDto> Create([FromBody] CreateUserRequestDto request)
    {
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

    /// <summary>
    /// Deletes a user by identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>204 No Content when the user is deleted; 404 Not Found when the user does not exist.</returns>
    /// <response code="204">The user was deleted successfully.</response>
    /// <response code="404">No user with the specified ID was found.</response>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(long id)
    {
        var deleted = userService.Delete(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }

}
