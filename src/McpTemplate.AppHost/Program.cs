var builder = DistributedApplication.CreateBuilder(args);

var toolServer = builder.AddProject<Projects.McpTemplate_ToolServer>("toolServer")
    .WithExternalHttpEndpoints();

var chatClient = builder.AddProject<Projects.McpTemplate_Console>("chatClient")
    .WaitFor(toolServer)
    .WithEnvironment("McpTemplateOptions__McpServers__0__Url", toolServer.GetEndpoint("http"))
    .WithExplicitStart()
    .ExcludeFromManifest();

builder.Build().Run();
