using System.ComponentModel.DataAnnotations;

namespace UserManagement.Shared.DTOs;

public sealed record UserListDto(IReadOnlyList<UserListItemDto> Items);

public sealed record UserListItemDto(
    long Id,
    string? Forename,
    string? Surname,
    string? Email,
    bool IsActive,
    DateTime DateOfBirth
);

public sealed record CreateUserRequestDto : IValidatableObject
{
    [Required, StringLength(100)] public string Forename { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Surname { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = string.Empty;
    [Required] public DateTime DateOfBirth { get; set; }
    public bool IsActive { get; set; }

    public CreateUserRequestDto(string forename, string surname, string email, DateTime dateOfBirth, bool isActive)
    {
        Forename = forename;
        Surname = surname;
        Email = email;
        DateOfBirth = dateOfBirth;
        IsActive = isActive;
    }

    // Parameterless ctor for Blazor binding
    public CreateUserRequestDto() { }

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
