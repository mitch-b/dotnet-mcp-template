namespace McpTemplate.Common.Models;

public class McpServerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "http"; // "http" or "stdio"
    public string? Url { get; set; } // for http
    public string? Command { get; set; } // for stdio
    public string? Image { get; set; } // for docker stdio
    public string? Tag { get; set; } // for docker stdio
    public List<string>? Args { get; set; } // for stdio or docker
}
