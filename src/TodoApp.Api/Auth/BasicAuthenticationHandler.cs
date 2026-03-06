using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TodoApp.Api.Auth;

public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!AuthenticationHeaderValue.TryParse(authHeaderValue, out var authHeader) ||
            !"Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authHeader.Parameter))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic authentication header."));
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic authentication payload."));
        }

        var parts = decoded.Split(':', 2);
        if (parts.Length != 2)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic authentication payload."));
        }

        var configuredUser = Context.RequestServices.GetRequiredService<IConfiguration>()["BasicAuth:User"] ?? "basic-user";
        var configuredPassword = Context.RequestServices.GetRequiredService<IConfiguration>()["BasicAuth:Password"] ?? "basic-pass";

        if (!string.Equals(parts[0], configuredUser, StringComparison.Ordinal) ||
            !string.Equals(parts[1], configuredPassword, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, parts[0]),
            new(ClaimTypes.Name, parts[0]),
            new(ClaimTypes.Role, "Operator")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}