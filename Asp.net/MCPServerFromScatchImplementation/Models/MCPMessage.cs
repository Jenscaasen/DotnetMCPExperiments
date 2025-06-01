namespace AspNetApiSse.Models;

/// <summary>
/// Represents an MCP (Model Context Protocol) message following JSON-RPC 2.0 specification
/// </summary>
public class MCPMessage
{
    public string? method { get; set; }
    public object? @params { get; set; }
    public object? id { get; set; } // Changed from string? to object? to handle both string and number IDs
    
    // For backward compatibility with property names
    public string? Method => method;
    public object? Params => @params;
    public object? Id => id;
}