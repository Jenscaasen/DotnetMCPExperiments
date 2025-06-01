using System.Text.Json.Serialization;

namespace McpStreamFunc.Models;

/// <summary>
/// Tool call parameter models for MCP tool execution
/// </summary>
public class ToolCallParams
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("arguments")]
    public object? Arguments { get; set; }
    
    [JsonPropertyName("_meta")]
    public object? Meta { get; set; }
}

/// <summary>
/// Arguments for the GreetUser tool
/// </summary>
public class GreetUserArgs
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}