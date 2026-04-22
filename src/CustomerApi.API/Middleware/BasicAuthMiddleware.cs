using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using CustomerApi.API.Options;
using Microsoft.Extensions.Options;

namespace CustomerApi.API.Middleware;

public sealed class BasicAuthMiddleware(
    RequestDelegate next,
    IOptions<BasicAuthOptions> options,
    ILogger<BasicAuthMiddleware> logger)
{
    private const string SwaggerPath  = "/swagger";
    private const string BasicScheme  = "Basic";
    private const string WwwAuthValue = $"{BasicScheme} realm=\"CustomerApi\"";

    private readonly string _username = options.Value.Username;
    private readonly string _password = options.Value.Password;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith(SwaggerPath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader)
            || !TryParseCredentials(authHeader.ToString(), out var username, out var password)
            || !SecureEquals(username, _username)
            || !SecureEquals(password, _password))
        {
            logger.LogWarning("Unauthorized request: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            context.Response.Headers.WWWAuthenticate = WwwAuthValue;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await next(context);
    }

    private static bool TryParseCredentials(
        string authHeader, out string username, out string password)
    {
        username = password = string.Empty;
        try
        {
            if (!AuthenticationHeaderValue.TryParse(authHeader, out var header)
                || !string.Equals(header.Scheme, BasicScheme, StringComparison.OrdinalIgnoreCase)
                || header.Parameter is null)
                return false;

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex < 0) return false;

            username = decoded[..separatorIndex];
            password = decoded[(separatorIndex + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool SecureEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
}
