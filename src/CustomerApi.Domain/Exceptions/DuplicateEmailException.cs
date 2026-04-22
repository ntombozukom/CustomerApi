namespace CustomerApi.Domain.Exceptions;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email)
        : base($"A customer with email '{email}' already exists.") { }
}
