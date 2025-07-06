using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.ClientModel;
using McpTemplate.Application.Extensions;
using McpTemplate.Common.Models;
using McpTemplate.Console;
using McpTemplate.Common.Interfaces;
using McpTemplate.Console.Handlers;

var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddUserSecrets<Program>();

builder.Services.Configure<McpTemplateOptions>(builder.Configuration.GetSection(nameof(McpTemplateOptions)));

builder.Services.AddSingleton<IOAuthAuthorizationHandler, OAuthAuthorizationHandler>();
builder.Services.AddApplicationServices(builder.Configuration);

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
