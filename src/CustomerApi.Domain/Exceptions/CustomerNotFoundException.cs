namespace CustomerApi.Domain.Exceptions;

public sealed class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(Guid id)
        : base($"Customer with ID '{id}' was not found.") { }
}
