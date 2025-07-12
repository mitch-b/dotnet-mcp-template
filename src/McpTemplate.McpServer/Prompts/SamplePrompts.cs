using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTemplate.McpServer.Prompts;

[McpServerPromptType]
public class SamplePrompts()
{
    [McpServerPrompt, Description("Getting date and time while using a tool")]
    public static ChatMessage GetDateTimePrompt() => new(ChatRole.User,
        "What is the current date and time using a tool?");
}
