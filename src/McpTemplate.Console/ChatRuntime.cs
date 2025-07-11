using McpTemplate.Common.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace McpTemplate.Console;

internal class ChatRuntime(
    ILogger<ChatRuntime> logger,
    IChatClient chatClient,
    IOptions<McpTemplateOptions> options,
    IServiceProvider serviceProvider) : IHostedService
{
    private readonly List<ChatMessage> _chatMessages = [];

    private ChatOptions? _chatOptions = null;

    private async Task RunConsoleLoop(CancellationToken cancellationToken)
    {
        System.Console.WriteLine("Console chat client ");
        System.Console.WriteLine("Type 'exit' to quit the application.");
        System.Console.WriteLine();

        _chatMessages.Add(new(ChatRole.System, GetSystemPrompt()));

        while (!cancellationToken.IsCancellationRequested)
        {
            System.Console.Write("[USER]: ");
            var userInput = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            // Process the user input (e.g., send it to the chat client)
            // _logger.LogInformation($"User Input: {userInput}");
            _chatMessages.Add(new ChatMessage(ChatRole.User, userInput));

            var response = await chatClient.GetResponseAsync(_chatMessages, _chatOptions, cancellationToken);
            if (response is null)
            {
                System.Console.WriteLine("No response received.");
                continue;
            }

            // _logger.LogInformation($"Response: {response.Text}");
            _chatMessages.Add(new ChatMessage(ChatRole.Assistant, response.Text));

            System.Console.WriteLine($"[LLM]: {response.Text}");
            System.Console.WriteLine();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _chatOptions = new ChatOptions();
        logger.LogInformation("Loading MCP tools...");
        await LoadAvailableTools(_chatOptions);
        logger.LogInformation("Starting Console Chat Runtime...");
        await RunConsoleLoop(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping");
        return Task.CompletedTask;
    }

    private string GetSystemPrompt()
    {
        return string.Join(Environment.NewLine, [
            "You are a helpful assistant .",
            "Assist by using tools available to you to fulfill the request.",
        ]);
    }

    private async Task LoadAvailableTools(ChatOptions chatOptions)
    {
        var mcpServers = options.Value.McpServers;
        var keys = mcpServers.Select(s => s.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var allTools = new List<AIFunction>();
        foreach (var key in keys)
        {
            try
            {
                var client = serviceProvider.GetKeyedService<IMcpClient>(key);
                if (client is not null)
                {
                    var tools = await client.ListToolsAsync();
                    allTools.AddRange(tools);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing '{Key}' MCP client: {ExMessage}", key, ex.Message);
            }
        }
        chatOptions.Tools = [.. allTools];
    }
}