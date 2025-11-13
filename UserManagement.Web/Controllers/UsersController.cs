using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
public class UsersController(IUserService userService, IUserLogService userLogService, ILogger<UsersController> logger) : ControllerBase
{
    /// <summary>
    /// Validates a Create/Update user request using data annotations and prepares a problem details
    /// object when invalid.
    /// </summary>
    /// <param name="request">The request DTO to validate.</param>
    /// <param name="problem">The populated ValidationProblemDetails when invalid; otherwise null.</param>
    /// <returns>True when valid; otherwise false.</returns>
    private bool TryValidateRequest(CreateUserRequestDto request, out ValidationProblemDetails? problem)
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
            problem = new(ModelState);
            logger.LogWarning("Create user validation failed");
            return false;
        }

        problem = null;
        return true;
    }

    /// <summary>
    /// Retrieves the full list of users.
    /// </summary>
    /// <returns>
    /// 200 OK with a <see cref="UserListDto"/> containing all users.
    /// </returns>
    /// <response code="200">Returns the list of users.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserListDto>> List()
    {
        logger.LogInformation("Listing all users");
        var users = await userService.GetAll().ToListAsync();
        var items = users.Select(Mappers.Map).ToList();

        var dto = new UserListDto(items);
        logger.LogInformation("Listed all users. Count: {Count}", dto.Items.Count);
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
    public async Task<ActionResult<UserListDto>> ListByActive([FromQuery(Name = "active")] bool isActive)
    {
        logger.LogInformation("Listing users by active filter. Active: {IsActive}", isActive);
        var users = await userService.FilterByActive(isActive).ToListAsync();
        var items = users.Select(Mappers.Map).ToList();

        var dto = new UserListDto(items);
        logger.LogInformation("Listed users by active filter. Active: {IsActive}. Count: {Count}", isActive, dto.Items.Count);
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
    public async Task<ActionResult<UserListItemDto>> GetById(long id)
    {
        using var scope = BeginUserScope(id);
        logger.LogInformation("Getting user by id {UserId}", id);
        var entity = await userService.GetAll().FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null)
        {
            logger.LogWarning("User not found for id {UserId}", id);
            return NotFound();
        }

        logger.LogInformation("Retrieved user id {UserId}", id);
        return Ok(entity.Map());
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">The identifier of the user to update.</param>
    /// <param name="request">The updated values for the user.</param>
    /// <returns>200 OK with the updated user, 404 if not found, or 400 if validation fails.</returns>
    /// <response code="200">The user was updated successfully.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="404">No user with the specified ID was found.</response>
    [HttpPut("{id:long}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserListItemDto>> Update(long id, [FromBody] CreateUserRequestDto request)
    {
        using var scope = BeginUserScope(id);
        if (!TryValidateRequest(request, out var problem))
        {
            logger.LogWarning("Update validation failed for user id {UserId}. Errors: {Errors}", id, problem?.Errors);
            return BadRequest(problem);
        }

        var entity = await userService.GetAll().FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null)
        {
            logger.LogWarning("Cannot update. User not found for id {UserId}", id);
            return NotFound();
        }

        var changed = false;
        if (!string.Equals(entity.Forename, request.Forename, StringComparison.Ordinal))
        { entity.Forename = request.Forename; changed = true; }
        if (!string.Equals(entity.Surname, request.Surname, StringComparison.Ordinal))
        { entity.Surname = request.Surname; changed = true; }
        if (!string.Equals(entity.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        { entity.Email = request.Email; changed = true; }
        if (entity.IsActive != request.IsActive)
        { entity.IsActive = request.IsActive; changed = true; }
        if (entity.DateOfBirth != request.DateOfBirth)
        { entity.DateOfBirth = request.DateOfBirth; changed = true; }

        if (!changed)
        {
            logger.LogInformation("No changes detected for user id {UserId}. Skipping update.", id);
        }

        var updated = changed ? await userService.UpdateAsync(entity) : entity;
        logger.LogInformation("Updated user id {UserId}. Changes applied: {Changed}", id, changed);
        return Ok(updated.Map());
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
    public async Task<ActionResult<UserListItemDto>> Create([FromBody] CreateUserRequestDto request)
    {
        if (!TryValidateRequest(request, out var problem))
            return BadRequest(problem);

        // Check for duplicate email (case-insensitive)
        var existing = await userService.GetAll().FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (existing != null)
        {
            var dupProblem = new ValidationProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "A user with this email already exists."
            };
            dupProblem.Errors[nameof(CreateUserRequestDto.Email)] = ["Email must be unique."];
            return BadRequest(dupProblem);
        }

        var user = new User
        {
            Forename = request.Forename,
            Surname = request.Surname,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            IsActive = request.IsActive
        };
        var created = await userService.AddAsync(user);
        logger.LogInformation($"Created user id {created.Id} with email {created.Email}");
        // Persist a user log for audit trail
        await userLogService.AddAsync(new UserLog
        {
            UserId = created.Id,
            Message = $"Created user id {created.Id}: {created.Forename} {created.Surname} ({created.Email})",
            CreatedAt = DateTime.UtcNow
        });
        var dto = new UserListItemDto(created.Id, created.Forename, created.Surname, created.Email, created.IsActive, created.DateOfBirth);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
    }

    /// <summary>
    /// Retrieves user logs for the specified user id with paging.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="page">The 1-based page number (defaults to 1).</param>
    /// <param name="pageSize">The number of items per page (defaults to 20, max 100).</param>
    /// <returns>200 OK with a paged list of <see cref="UserLog"/> entries.</returns>
    /// <response code="200">Returns the paged list of logs.</response>
    [HttpGet("{id:long}/logs")]
    [ProducesResponseType(typeof(PagedResultDto<UserLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<UserLogDto>>> GetLogs(long id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        using var scope = BeginUserScope(id);
        // Normalize paging inputs
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 20;
        const int maxPageSize = 100;
        if (pageSize > maxPageSize) pageSize = maxPageSize;

        var logsQuery = userLogService
            .GetByUserId(id)
            .OrderByDescending(l => l.CreatedAt);

        var total = await logsQuery.CountAsync();

        var skip = (page - 1) * pageSize;
        var logs = await logsQuery.Skip(skip).Take(pageSize).ToListAsync();
        var items = logs.Select(Mappers.Map).ToList();

        logger.LogInformation("Listing logs for user {UserId}. Page: {Page}, PageSize: {PageSize}, Returned: {Count}", id, page, pageSize, items.Count);

        var result = new PagedResultDto<UserLogDto>(items, page, pageSize, total);
        return Ok(result);
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
    public async Task<ActionResult> Delete(long id)
    {
        using var scope = BeginUserScope(id);
        logger.LogInformation("Deleting user id {UserId}", id);
        var deleted = await userService.DeleteAsync(id);
        if (!deleted)
        {
            logger.LogWarning("Cannot delete. User not found for id {UserId}", id);
            return NotFound();
        }
        logger.LogInformation("Deleted user id {UserId}", id);
        return NoContent();
    }

    private IDisposable? BeginUserScope(long userId) =>
        logger.BeginScope(new Dictionary<string, object?> { ["UserId"] = userId });
}
