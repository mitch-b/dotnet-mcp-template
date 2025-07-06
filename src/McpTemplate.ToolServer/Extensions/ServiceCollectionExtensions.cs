namespace McpTemplate.ToolServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpServices(this IServiceCollection services, IConfiguration configuration, bool enableOAuth)
    {
        var mcpBuilder = services
            .AddMcpServer()
            .WithHttpTransport(opt =>
            {
                opt.Stateless = true;
            })
            .WithToolsFromAssembly();
        return services;
    }
}
