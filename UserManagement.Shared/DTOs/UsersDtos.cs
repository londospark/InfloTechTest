using System;

namespace UserManagement.Shared.DTOs;

// Immutable DTOs shared between Web API and Blazor client
public sealed record UserListDto(IReadOnlyList<UserListItemDto> Items);

public sealed record UserListItemDto(
    long Id,
    string? Forename,
    string? Surname,
    string? Email,
    bool IsActive,
    DateTime DateOfBirth
);

// Request DTO for creating a new user
public sealed record CreateUserRequestDto(
    string Forename,
    string Surname,
    string Email,
    DateTime DateOfBirth,
    bool IsActive
);
