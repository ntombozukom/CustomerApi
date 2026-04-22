namespace CustomerApi.Application.DTOs;

public interface ICustomerRequest
{
    string FirstName { get; }
    string LastName  { get; }
    string Email     { get; }
    int    Age       { get; }
}
