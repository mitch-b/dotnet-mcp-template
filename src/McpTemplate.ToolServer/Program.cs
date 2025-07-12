using McpTemplate.Application.Extensions;
using McpTemplate.ToolServer.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Derive the server URL for Resource metadata
var serverUrl = "http://localhost:5499/"; // trailing slash important here...
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

// Read OAuth config
var oauthSection = builder.Configuration.GetSection("OAuth");
var oauthAuthority = oauthSection["Authority"];
var oauthAudience = oauthSection["Audience"];
var oauthTenant = oauthSection["Tenant"];

var enableOAuth = !string.IsNullOrWhiteSpace(oauthAuthority)
    && !string.IsNullOrWhiteSpace(oauthAudience);

if (enableOAuth)
{
    string[] validAudiences = [serverUrl, $"api://{oauthAudience}"];
    string[] validIssuers = [oauthAuthority, $"https://sts.windows.net/{oauthTenant}/"];
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = oauthAuthority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudiences = validAudiences,
            ValidIssuers = validIssuers,
            NameClaimType = "name",
            RoleClaimType = "roles",
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var name = context.Principal?.Identity?.Name ?? "unknown";
                var email = context.Principal?.FindFirstValue("preferred_username") ?? "unknown";
                Console.WriteLine($"Token validated for: {name} ({email})");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"Challenging client to authenticate with Entra ID");
                return Task.CompletedTask;
            }
        };
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            Resource = new Uri(serverUrl),
            ResourceDocumentation = new Uri("https://docs.example.com/api/McpTemplate"),
            AuthorizationServers = { new Uri(oauthAuthority!) },
            ScopesSupported = [$"api://{oauthAudience}/mcp.tools"]
        };
    });

    builder.Services.AddAuthorization();
}

builder.Services.AddHttpContextAccessor();

// McpTemplate.Application.Extensions
builder.Services.AddApplicationServices(builder.Configuration);
// McpTemplate.ToolServer.Extensions
builder.Services.AddMcpServices(builder.Configuration, enableOAuth);

var app = builder.Build();
app.UseExceptionHandler();

app.UseRouting();
app.MapDefaultEndpoints();

app.MapGet("/", () => "McpTemplate MCP server is running!");

if (enableOAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapMcp().RequireAuthorization();
}
else
{
    app.MapMcp();
}

app.Run();
