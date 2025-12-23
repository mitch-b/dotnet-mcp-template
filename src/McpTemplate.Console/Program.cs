using System.ClientModel;
using Azure.AI.OpenAI;
using McpTemplate.Application.Extensions;
using McpTemplate.Common.Interfaces;
using McpTemplate.Common.Models;
using McpTemplate.Console;
using McpTemplate.Console.Handlers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddUserSecrets<Program>();

builder.Services.Configure<McpTemplateOptions>(builder.Configuration.GetSection(nameof(McpTemplateOptions)));
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));

builder.Services.AddSingleton<IOAuthAuthorizationHandler, OAuthAuthorizationHandler>();
builder.Services.AddMcpClients(builder.Configuration);

await using var serviceProvider = builder.Services.BuildServiceProvider();
var options = serviceProvider.GetRequiredService<IOptions<McpTemplateOptions>>().Value;

var innerChatClient = new AzureOpenAIClient(new Uri(options.Endpoint!), new ApiKeyCredential(options.ApiKey!))
    .GetChatClient(options.Model!)
    .AsIChatClient();
builder.Services.AddChatClient(innerChatClient)
    .UseFunctionInvocation();

builder.Services.AddHostedService<ChatRuntime>();

var app = builder.Build();

await app.RunAsync();