using McpTemplate.ToolServer.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using McpTemplate.Application.Extensions;
using System.Security.Claims;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Derive the server URL for Resource metadata
var serverUrl = builder.Configuration["applicationUrl"]
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    ?? "http://localhost:5499";
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

// Read OAuth config
var oauthSection = builder.Configuration.GetSection("OAuth");
var oauthAuthority = oauthSection["Authority"];
var oauthAudience = oauthSection["Audience"];

var enableOAuth = !string.IsNullOrWhiteSpace(oauthAuthority)
    && !string.IsNullOrWhiteSpace(oauthAudience);

if (enableOAuth)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = oauthAuthority;
        // options.Audience = oauthAudience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = oauthAudience,
            ValidIssuer = oauthAuthority,
            NameClaimType = "name",
            RoleClaimType = "roles"
        };

        options.MetadataAddress = $"{oauthAuthority}/.well-known/openid-configuration";

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
            BearerMethodsSupported = { "header" },
            ResourceDocumentation = new Uri("https://docs.example.com/api/McpTemplate"),
            AuthorizationServers = { new Uri(oauthAuthority!) },
            ScopesSupported = [ $"api://{oauthAudience}/mcp.tools" ]
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
