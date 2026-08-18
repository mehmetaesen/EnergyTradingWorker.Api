using System.Security.Cryptography;
using System.Text;

namespace EnergyTrading.Worker;

public sealed class OperationsAuthenticationMiddleware(
    RequestDelegate next,
    IWebHostEnvironment environment,
    IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsProtectedPath(context.Request.Path) || environment.IsDevelopment())
        {
            await next(context);
            return;
        }

        var expectedUsername = configuration["WorkerOperation:Username"];
        var expectedPassword = configuration["WorkerOperation:Password"];
        if (string.IsNullOrWhiteSpace(expectedUsername) || string.IsNullOrWhiteSpace(expectedPassword))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Operations UI credentials are not configured.");
            return;
        }

        if (!TryReadBasicCredentials(context.Request, out var username, out var password)
            || !FixedTimeEquals(username, expectedUsername)
            || !FixedTimeEquals(password, expectedPassword))
        {
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"EnergyTrading Operations\", charset=\"UTF-8\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private static bool IsProtectedPath(PathString path) =>
        path == "/" || path == "/index.html" || path.StartsWithSegments("/api/jobs") || path.StartsWithSegments("/hangfire");

    private static bool TryReadBasicCredentials(HttpRequest request, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;
        var value = request.Headers.Authorization.ToString();
        if (!value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value[6..].Trim()));
            var separator = decoded.IndexOf(':');
            if (separator < 1) return false;
            username = decoded[..separator];
            password = decoded[(separator + 1)..];
            return true;
        }
        catch (FormatException) { return false; }
    }

    private static bool FixedTimeEquals(string actual, string expected)
    {
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return actualBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
