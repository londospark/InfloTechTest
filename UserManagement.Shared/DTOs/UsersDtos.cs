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
