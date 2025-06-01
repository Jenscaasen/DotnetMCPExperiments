namespace AspNetApiSse.Models;

/// <summary>
/// Tool call parameter models for MCP tool execution
/// </summary>
public class ToolCallParams
{
    public string? name { get; set; }
    public object? arguments { get; set; }
    public object? _meta { get; set; }
}

/// <summary>
/// Arguments for the GreetUser tool
/// </summary>
public class GreetUserArgs
{
    public string? name { get; set; }
    public string? title { get; set; }
}

/// <summary>
/// Arguments for the EchoTool
/// </summary>
public class EchoToolArgs
{
    public string? message { get; set; }
}