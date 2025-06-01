using McpStreamFunc.Models;

namespace McpStreamFunc.Services;

/// <summary>
/// Service for processing legacy event messages (kept for backward compatibility)
/// </summary>
public class LegacyEventProcessor
{
    private readonly MCPToolsService _toolsService;

    public LegacyEventProcessor(MCPToolsService toolsService)
    {
        _toolsService = toolsService;
    }

    public object ProcessEvent(EventMessage eventMessage)
    {
        return eventMessage.Method?.ToLower() switch
        {
            "initialize" => new {
                capabilities = new {
                    tools = new {
                        listChanged = false
                    }
                },
                serverInfo = new {
                    name = "Custom Streamable HTTP MCP server",
                    version = "1.0.0"
                }
            },
            "tools/list" => _toolsService.ListTools(),
            "ping" => new { method = "pong", message = "Ping received successfully", originalData = eventMessage.Data },
            "echo" => new { method = "echo", message = eventMessage.Data ?? "No data provided" },
            "status" => new { method = "status", message = "System is running", uptime = Environment.TickCount64 },
            "time" => new { method = "time", message = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
            _ => new { 
                method = "unknown", 
                message = $"Unknown method: {eventMessage.Method}", 
                supportedMethods = new[] { "initialize", "tools/list", "ping", "echo", "status", "time" } 
            }
        };
    }
}