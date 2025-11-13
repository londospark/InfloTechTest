using System.ComponentModel.DataAnnotations;

namespace UserManagement.Shared.DTOs;

/// <summary>
/// A paged-style container for user items returned by the Users API.
/// </summary>
public sealed record UserListDto
{
    /// <summary>
    /// The collection of users returned by the request.
    /// </summary>
    public IReadOnlyList<UserListItemDto> Items { get; init; }

    /// <summary>
    /// Creates a new <see cref="UserListDto"/> with the specified items.
    /// </summary>
    /// <param name="items">The user items to include in the response.</param>
    public UserListDto(IReadOnlyList<UserListItemDto> items) => Items = items;
}

/// <summary>
/// A lightweight view of a user returned from list and read endpoints.
/// </summary>
public sealed record UserListItemDto
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// The user's given name.
    /// </summary>
    public string? Forename { get; init; }

    /// <summary>
    /// The user's family name.
    /// </summary>
    public string? Surname { get; init; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Indicates whether the user is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// The user's date of birth.
    /// </summary>
    public DateTime DateOfBirth { get; init; }

    /// <summary>
    /// Creates a new <see cref="UserListItemDto"/> instance.
    /// </summary>
    public UserListItemDto(long id, string? forename, string? surname, string? email, bool isActive, DateTime dateOfBirth)
    {
        Id = id;
        Forename = forename;
        Surname = surname;
        Email = email;
        IsActive = isActive;
        DateOfBirth = dateOfBirth;
    }
}

/// <summary>
/// The payload used to create a user via the API.
/// </summary>
/// <remarks>
/// All properties are validated. Invalid requests will return a 400 response with validation details.
/// </remarks>
public sealed record CreateUserRequestDto : IValidatableObject
{
    /// <summary>
    /// The user's given name.
    /// </summary>
    [Required, StringLength(100)]
    public string Forename { get; set; } = string.Empty;

    /// <summary>
    /// The user's family name.
    /// </summary>
    [Required, StringLength(100)]
    public string Surname { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's date of birth.
    /// </summary>
    [Required]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Whether the user should be created as active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creates a new <see cref="CreateUserRequestDto"/>.
    /// </summary>
    public CreateUserRequestDto(string forename, string surname, string email, DateTime dateOfBirth, bool isActive)
    {
        Forename = forename;
        Surname = surname;
        Email = email;
        DateOfBirth = dateOfBirth;
        IsActive = isActive;
    }

    /// <summary>
    /// Parameterless constructor for model binding.
    /// </summary>
    public CreateUserRequestDto() { }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth > DateTime.Now)
        {
            yield return new(
                "Date of Birth cannot be in the future.",
                [nameof(DateOfBirth)]
            );
        }
    }
}

/// <summary>
/// A DTO representing a user log entry.
/// </summary>
public sealed record UserLogDto
{
    /// <summary>
    /// The unique identifier for the log entry.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// The identifier of the user this log entry relates to.
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// The log message or description.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The UTC timestamp when the log was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="UserLogDto"/> instance.
    /// </summary>
    public UserLogDto(long id, long userId, string message, DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        Message = message;
        CreatedAt = createdAt;
    }
}
