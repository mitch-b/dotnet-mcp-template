namespace McpTemplate.ToolServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddMcpServer()
            .WithHttpTransport(opt =>
            {
            })
            .WithToolsFromAssembly();
        return services;
    }
}
