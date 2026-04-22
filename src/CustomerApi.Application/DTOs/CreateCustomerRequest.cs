namespace CustomerApi.Application.DTOs;

public sealed record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    int Age) : ICustomerRequest;
