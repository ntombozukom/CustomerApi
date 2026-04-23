namespace CustomerApi.Application.DTOs;

public sealed record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    int Age,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
