using System.Text.Json.Serialization;

namespace McpStreamFunc.Models;

/// <summary>
/// Represents an MCP (Model Context Protocol) message following JSON-RPC 2.0 specification
/// </summary>
public class MCPMessage
{
    [JsonPropertyName("method")]
    public string? Method { get; set; }
    
    [JsonPropertyName("params")]
    public object? Params { get; set; }
    
    [JsonPropertyName("id")]
    public object? Id { get; set; } // Changed from string? to object? to handle both string and number IDs
}