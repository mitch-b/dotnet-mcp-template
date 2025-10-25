using McpTemplate.Common.Interfaces;
using McpTemplate.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpTemplate.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var mcpServersConfig =
            configuration.GetSection(nameof(McpTemplateOptions)).Get<McpTemplateOptions>() ?? new();

        // Read OAuth config if present
        var oauthOptions = configuration.GetSection("OAuth").Get<OAuthOptions>();
        var hasOAuth = oauthOptions != null &&
            !string.IsNullOrWhiteSpace(oauthOptions.Authority) &&
            !string.IsNullOrWhiteSpace(oauthOptions.ClientId);

        foreach (var server in mcpServersConfig.McpServers)
        {
            services.AddKeyedSingleton<IMcpClient>(server.Name, (serviceProvider, _) =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var clientOptions = new McpClientOptions
                {
                    ClientInfo = new Implementation
                    {
                        Name = server.Name,
                        Version = "1.0.0"
                    },
                };
                switch (server.Type.ToLowerInvariant())
                {
                    case "http":
                        var url = server.Url;
                        if (string.IsNullOrEmpty(url))
                        {
                            throw new InvalidOperationException($"No URL configured for MCP server '{server.Name}'");
                        }

                        var sharedHandler = new SocketsHttpHandler
                        {
                            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
                        };
                        var client = new HttpClient(sharedHandler) { BaseAddress = new Uri(url) };
                        var httpTransportOptions = new SseClientTransportOptions
                        {
                            Endpoint = client.BaseAddress!,
                            TransportMode = HttpTransportMode.AutoDetect,
                        };
                        SseClientTransport httpClientTransport;
                        if (hasOAuth)
                        {
                            httpTransportOptions.OAuth = new()
                            {
                                ClientName = "McpTemplate Client",
                                ClientId = oauthOptions?.ClientId,
                                RedirectUri = !string.IsNullOrWhiteSpace(oauthOptions?.RedirectUri) ? new Uri(oauthOptions.RedirectUri) : null!,
                                Scopes = oauthOptions?.Scopes ?? [],
                                AuthorizationRedirectDelegate = async (authorizationUrl, redirectUri, cancellationToken) =>
                                {
                                    var handler = serviceProvider.GetRequiredService<IOAuthAuthorizationHandler>();
                                    return await handler.HandleAuthorizationUrlAsync(authorizationUrl, redirectUri, cancellationToken);
                                }
                            };
                        }
                        httpClientTransport = new SseClientTransport(httpTransportOptions, client, loggerFactory);
                        return McpClientFactory.CreateAsync(httpClientTransport, clientOptions, loggerFactory).GetAwaiter().GetResult();
                    case "stdio":
                        string command;
                        List<string> args = new();
                        if (!string.IsNullOrWhiteSpace(server.Image))
                        {
                            // Docker stdio
                            var image = server.Image;
                            var tag = string.IsNullOrWhiteSpace(server.Tag) ? "latest" : server.Tag;
                            command = "docker run";
                            args.Add("--rm");
                            args.Add("-i");
                            if (server.Args != null)
                            {
                                args.AddRange(server.Args);
                            }
                            args.Add($"{image}:{tag}");
                        }
                        else if (!string.IsNullOrEmpty(server.Command))
                        {
                            command = server.Command;
                            if (server.Args != null)
                            {
                                args.AddRange(server.Args);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"No command or image configured for stdio MCP server '{server.Name}'");
                        }
                        var stdioOptions = new StdioClientTransportOptions
                        {
                            Name = server.Name,
                            Command = command,
                            Arguments = [.. args]
                        };
                        var stdioTransport = new StdioClientTransport(stdioOptions);
                        return McpClientFactory.CreateAsync(stdioTransport, clientOptions, loggerFactory).GetAwaiter().GetResult();
                    default:
                        throw new InvalidOperationException($"Unknown MCP server type '{server.Type}' for '{server.Name}'");
                }
            });
        }
    }
}