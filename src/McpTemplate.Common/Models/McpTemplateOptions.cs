namespace McpTemplate.Common.Models;

public class McpTemplateOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public List<McpServerConfiguration> McpServers { get; set; } = new();
}