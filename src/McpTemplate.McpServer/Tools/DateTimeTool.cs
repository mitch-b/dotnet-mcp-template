using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTemplate.McpServer.Tools;

[McpServerToolType]
public class DateTimeTool(ILogger<DateTimeTool> logger)
{
    [McpServerTool, Description("Get current date time")]
    public string GetDateTime(IHttpContextAccessor httpContextAccessor)
    {
        var name = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "unknown";
        logger.LogInformation("Current datetime requested by {Name}", name);
        return $"Hi, {name}. The current date and time is {DateTime.Now:s}.";
    }
}
