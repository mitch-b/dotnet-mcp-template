namespace McpTemplate.McpServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpServices(this IServiceCollection services, IConfiguration configuration, bool enableOAuth)
    {
        var mcpBuilder = services
            .AddMcpServer()
            .WithHttpTransport(opt =>
            {
                opt.Stateless = true;
                // opt.ConfigureSessionOptions = (httpContext, mcpServerOptions, cancellationToken) =>
                // {
                //     return Task.CompletedTask;
                // };
            })
            .WithToolsFromAssembly();
        return services;
    }
}
