using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTemplate.ToolServer.Tools;

[McpServerToolType]
public class DateTimeTool(ILogger<DateTimeTool> logger)
{
    [McpServerTool, Description("Get current date time")]
    public string GetDateTime(IHttpContextAccessor httpContextAccessor)
    {
        logger.LogInformation("Current datetime requested");
        return DateTime.Now.ToString("s");
    }
}
