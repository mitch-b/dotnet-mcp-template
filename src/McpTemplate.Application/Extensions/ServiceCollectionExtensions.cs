using McpTemplate.Common.Interfaces;
using McpTemplate.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpTemplate.Application.Extensions;

/// <summary>
/// Extension methods for configuring MCP (Model Context Protocol) services.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string DefaultClientVersion = "1.0.0";
    private const string HttpTransportType = "http";
    private const string StdioTransportType = "stdio";

    /// <summary>
    /// Adds MCP client services to the service collection based on configuration.
    /// Registers each configured MCP server as a keyed singleton <see cref="McpClient"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration containing MCP server settings.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Configuration is read from the "McpTemplateOptions" section for server definitions
    /// and the "OAuth" section for optional OAuth settings.
    /// </para>
    /// <para>
    /// Each MCP server is registered as a keyed singleton using the server name as the key.
    /// Use <c>serviceProvider.GetKeyedService&lt;McpClient&gt;(serverName)</c> to resolve.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// services.AddMcpClients(configuration);
    ///
    /// // Later, to resolve a specific client:
    /// var client = serviceProvider.GetKeyedService&lt;McpClient&gt;("my-server-name");
    /// </code>
    /// </example>
    public static IServiceCollection AddMcpClients(this IServiceCollection services, IConfiguration configuration)
    {
        var mcpOptions = configuration
            .GetSection(nameof(McpTemplateOptions))
            .Get<McpTemplateOptions>() ?? new();

        var oauthOptions = configuration
            .GetSection("OAuth")
            .Get<OAuthOptions>();

        foreach (var server in mcpOptions.McpServers)
        {
            RegisterMcpClient(services, server, oauthOptions);
        }

        return services;
    }

    /// <summary>
    /// Registers a single MCP client as a keyed singleton service.
    /// </summary>
    private static void RegisterMcpClient(
        IServiceCollection services,
        McpServerConfiguration server,
        OAuthOptions? oauthOptions)
    {
        services.AddKeyedSingleton<McpClient>(server.Name, (serviceProvider, _) =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var clientOptions = CreateClientOptions(server);

            var transport = CreateTransport(server, oauthOptions, serviceProvider, loggerFactory);

            // Note: Using GetAwaiter().GetResult() here because DI factory is synchronous.
            // Consider using IHostedService for async initialization if this becomes problematic.
            return McpClient.CreateAsync(transport, clientOptions, loggerFactory).GetAwaiter().GetResult();
        });
    }

    /// <summary>
    /// Creates MCP client options for the specified server configuration.
    /// </summary>
    private static McpClientOptions CreateClientOptions(McpServerConfiguration server)
    {
        return new McpClientOptions
        {
            ClientInfo = new Implementation
            {
                Name = server.Name,
                Version = DefaultClientVersion
            }
        };
    }

    /// <summary>
    /// Creates the appropriate transport based on server configuration.
    /// </summary>
    private static IClientTransport CreateTransport(
        McpServerConfiguration server,
        OAuthOptions? oauthOptions,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        return server.Type.ToLowerInvariant() switch
        {
            HttpTransportType => CreateHttpTransport(server, oauthOptions, serviceProvider, loggerFactory),
            StdioTransportType => CreateStdioTransport(server),
            _ => throw new InvalidOperationException(
                $"Unknown MCP server type '{server.Type}' for '{server.Name}'. " +
                $"Supported types: '{HttpTransportType}', '{StdioTransportType}'.")
        };
    }

    /// <summary>
    /// Creates an HTTP transport for connecting to an MCP server over HTTP/SSE.
    /// </summary>
    private static HttpClientTransport CreateHttpTransport(
        McpServerConfiguration server,
        OAuthOptions? oauthOptions,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrEmpty(server.Url))
        {
            throw new InvalidOperationException(
                $"No URL configured for HTTP MCP server '{server.Name}'. " +
                "Set the 'Url' property in your configuration.");
        }

        var httpClient = CreatePooledHttpClient(server.Url);
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = httpClient.BaseAddress!,
            TransportMode = HttpTransportMode.AutoDetect
        };

        ConfigureOAuthIfEnabled(transportOptions, oauthOptions, serviceProvider);

        return new HttpClientTransport(transportOptions, httpClient, loggerFactory);
    }

    /// <summary>
    /// Creates an HttpClient with connection pooling for optimal performance.
    /// </summary>
    private static HttpClient CreatePooledHttpClient(string url)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };

        return new HttpClient(handler) { BaseAddress = new Uri(url) };
    }

    /// <summary>
    /// Configures OAuth settings on the transport options if OAuth is enabled.
    /// </summary>
    private static void ConfigureOAuthIfEnabled(
        HttpClientTransportOptions transportOptions,
        OAuthOptions? oauthOptions,
        IServiceProvider serviceProvider)
    {
        if (!IsOAuthEnabled(oauthOptions))
        {
            return;
        }

        transportOptions.OAuth = new()
        {
            ClientId = oauthOptions!.ClientId,
            RedirectUri = ParseRedirectUri(oauthOptions.RedirectUri),
            Scopes = oauthOptions.Scopes ?? [],
            AuthorizationRedirectDelegate = async (authorizationUrl, redirectUri, cancellationToken) =>
            {
                var handler = serviceProvider.GetRequiredService<IOAuthAuthorizationHandler>();
                return await handler.HandleAuthorizationUrlAsync(authorizationUrl, redirectUri, cancellationToken);
            }
        };
    }

    /// <summary>
    /// Determines if OAuth is properly configured and should be enabled.
    /// </summary>
    private static bool IsOAuthEnabled(OAuthOptions? options) =>
        options is not null &&
        !string.IsNullOrWhiteSpace(options.Authority) &&
        !string.IsNullOrWhiteSpace(options.ClientId);

    /// <summary>
    /// Parses the redirect URI string into a Uri object.
    /// </summary>
    private static Uri? ParseRedirectUri(string? redirectUri) =>
        !string.IsNullOrWhiteSpace(redirectUri) ? new Uri(redirectUri) : null;

    /// <summary>
    /// Creates a stdio transport for connecting to an MCP server via standard I/O.
    /// Supports both direct command execution and Docker-based servers.
    /// </summary>
    private static StdioClientTransport CreateStdioTransport(McpServerConfiguration server)
    {
        var (command, arguments) = BuildStdioCommand(server);

        var options = new StdioClientTransportOptions
        {
            Name = server.Name,
            Command = command,
            Arguments = [.. arguments]
        };

        return new StdioClientTransport(options);
    }

    /// <summary>
    /// Builds the command and arguments for stdio transport based on configuration.
    /// </summary>
    private static (string Command, List<string> Arguments) BuildStdioCommand(McpServerConfiguration server)
    {
        // Docker-based stdio server
        if (!string.IsNullOrWhiteSpace(server.Image))
        {
            return BuildDockerCommand(server);
        }

        // Direct command execution
        if (!string.IsNullOrEmpty(server.Command))
        {
            var args = server.Args?.ToList() ?? [];
            return (server.Command, args);
        }

        throw new InvalidOperationException(
            $"No command or image configured for stdio MCP server '{server.Name}'. " +
            "Set either the 'Command' or 'Image' property in your configuration.");
    }

    /// <summary>
    /// Builds the Docker run command for container-based MCP servers.
    /// </summary>
    private static (string Command, List<string> Arguments) BuildDockerCommand(McpServerConfiguration server)
    {
        var tag = string.IsNullOrWhiteSpace(server.Tag) ? "latest" : server.Tag;
        var imageWithTag = $"{server.Image}:{tag}";

        var args = new List<string> { "run", "--rm", "-i" };

        if (server.Args is not null)
        {
            args.AddRange(server.Args);
        }

        args.Add(imageWithTag);

        return ("docker", args);
    }
}