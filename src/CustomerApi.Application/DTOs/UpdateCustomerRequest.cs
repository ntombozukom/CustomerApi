namespace CustomerApi.Application.DTOs;

public sealed record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    int Age) : ICustomerRequest;
