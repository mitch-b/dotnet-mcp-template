using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using McpTemplate.Common.Models;

namespace McpTemplate.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpTemplateOptions>>();
        var mcpServersConfig = options.Value;

        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

        foreach (var server in mcpServersConfig.McpServers)
        {
            services.AddKeyedSingleton<IMcpClient>(server.Name, (serviceCollection, _) =>
            {
                var clientOptions = new McpClientOptions
                {
                    ClientInfo = new Implementation
                    {
                        Name = server.Name,
                        Version = "1.0.0"
                    },
                    // Capabilities = new ClientCapabilities() { }
                };;
                switch (server.Type.ToLowerInvariant())
                {
                    case "http":
                        var url = server.Url;
                        if (string.IsNullOrEmpty(url))
                            throw new InvalidOperationException($"No URL configured for MCP server '{server.Name}'");
                        var client = new HttpClient { BaseAddress = new Uri(url) };
                        var httpTransportOptions = new SseClientTransportOptions
                        {
                            Endpoint = client.BaseAddress!,
                            TransportMode = HttpTransportMode.AutoDetect
                        };
                        var httpClientTransport = new SseClientTransport(httpTransportOptions);
                        return McpClientFactory.CreateAsync(httpClientTransport, clientOptions).GetAwaiter().GetResult();
                    case "stdio":
                        string command;
                        List<string> args = new();
                        if (!string.IsNullOrWhiteSpace(server.Image))
                        {
                            // Docker stdio
                            var image = server.Image;
                            var tag = string.IsNullOrWhiteSpace(server.Tag) ? "latest" : server.Tag;
                            command = "docker run";
                            //args.Add("run");
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
