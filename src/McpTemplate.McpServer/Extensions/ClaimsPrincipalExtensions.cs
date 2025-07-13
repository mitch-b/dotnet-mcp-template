using System.Security.Claims;

namespace McpTemplate.McpServer.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetGivenName(this ClaimsPrincipal claimsPrincipal) => claimsPrincipal
        .Claims
        .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?
        .Value;

    public static string? GetSurname(this ClaimsPrincipal claimsPrincipal) => claimsPrincipal
        .Claims
        .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?
        .Value;

    public static string? GetEmail(this ClaimsPrincipal claimsPrincipal) => claimsPrincipal
        .Claims
        .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?
        .Value;
}