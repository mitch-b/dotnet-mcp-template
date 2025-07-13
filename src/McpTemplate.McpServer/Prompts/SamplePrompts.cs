using Microsoft.Extensions.AI;
using McpTemplate.McpServer.Extensions;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTemplate.McpServer.Prompts;

[McpServerPromptType]
public class SamplePrompts()
{
    [McpServerPrompt(Name = "date_time"),
        Description("Getting date and time while using a tool")]
    public static ChatMessage GetDateTimePrompt(IHttpContextAccessor httpContextAccessor)
    {
        var firstName = httpContextAccessor.HttpContext?.User?.GetGivenName() ?? "[missing name]";
        var prompt = $"Hello - I'm {firstName}. Tell me the current date and time using a tool?";
        return new ChatMessage(ChatRole.User, prompt);
    }

    [McpServerPrompt(Name = "echo"),
        Description("Echoing back user input")]
    public static ChatMessage GetEchoPrompt(IHttpContextAccessor httpContextAccessor, string userInput)
    {
        var email = httpContextAccessor.HttpContext?.User?.GetEmail() ?? "[missing email]";
        var prompt = $"Hello - my email is {email}. Please say: '{userInput}'. Can you repeat that back to me?";
        return new ChatMessage(ChatRole.User, prompt);
    }
}
