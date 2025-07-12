using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTemplate.McpServer.Prompts;

[McpServerPromptType]
public class SamplePrompts()
{
    [McpServerPrompt, Description("Getting date and time while using a tool")]
    public static ChatMessage GetDateTimePrompt(IHttpContextAccessor httpContextAccessor)
    {
        var userName = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "User";
        var prompt = $"Hello - I'm {userName}. Tell me the current date and time using a tool?";
        return new ChatMessage(ChatRole.User, prompt);
    }
}
