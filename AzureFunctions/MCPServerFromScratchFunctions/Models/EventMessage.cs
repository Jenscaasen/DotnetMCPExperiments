namespace McpStreamFunc.Models;

/// <summary>
/// Event message model (kept for backward compatibility)
/// </summary>
public record EventMessage(string? Method, object? Data = null);