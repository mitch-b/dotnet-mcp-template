namespace McpTemplate.Common.Models;

public class OAuthOptions
{
    public string? Authority { get; set; }
    public string? Audience { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public List<string>? Scopes { get; set; }
}
