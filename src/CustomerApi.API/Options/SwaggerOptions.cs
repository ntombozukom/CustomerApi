namespace CustomerApi.API.Options;

public sealed class SwaggerOptions
{
    public const string SectionName = "Swagger";

    public string Title       { get; init; } = "Customer API";
    public string ContactName { get; init; } = string.Empty;
}
