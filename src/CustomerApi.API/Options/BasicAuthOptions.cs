namespace CustomerApi.API.Options;

public sealed class BasicAuthOptions
{
    public const string SectionName = "BasicAuth";

    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
