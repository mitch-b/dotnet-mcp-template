using McpTemplate.Application.Extensions;
using McpTemplate.Common.Models;
using McpTemplate.McpServer.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Derive the server URL for Resource metadata
// TODO: get from environment variable or configuration
var serverUrl = "http://localhost:5499/"; // trailing slash important here...
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));
await using var serviceProvider = builder.Services.BuildServiceProvider();
var oauthOptions = serviceProvider.GetRequiredService<IOptions<OAuthOptions>>().Value;

var enableOAuth = !string.IsNullOrWhiteSpace(oauthOptions.Authority)
    && !string.IsNullOrWhiteSpace(oauthOptions.Audience);

if (enableOAuth)
{
    string[] validAudiences = [$"{oauthOptions.Audience}"];
    string[] validIssuers = [$"https://sts.windows.net/{oauthOptions.Tenant}/", oauthOptions.Authority!];
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = oauthOptions.Authority;
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
            AuthorizationServers = { new Uri(oauthOptions.Authority!) },
            ScopesSupported = oauthOptions.Scopes ?? []
        };
    });

    builder.Services.AddAuthorization();
}

builder.Services.AddHttpContextAccessor();

// McpTemplate.Application.Extensions
builder.Services.AddApplicationServices(builder.Configuration);
// McpTemplate.McpServer.Extensions
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
