using McpStreamFunc.Models;
using Microsoft.Extensions.Logging;

namespace McpStreamFunc.Services;

/// <summary>
/// Service for processing MCP messages and routing them to appropriate handlers
/// </summary>
public class MCPMessageProcessor
{
    private readonly MCPToolsService _toolsService;
    private readonly MCPPromptsService _promptsService;
    private readonly MCPResourcesService _resourcesService;
    private readonly ILogger<MCPMessageProcessor> _logger;

    public MCPMessageProcessor(
        MCPToolsService toolsService,
        MCPPromptsService promptsService,
        MCPResourcesService resourcesService,
        ILogger<MCPMessageProcessor> logger)
    {
        _toolsService = toolsService;
        _promptsService = promptsService;
        _resourcesService = resourcesService;
        _logger = logger;
    }

    public object? ProcessMessage(MCPMessage? mcpMessage)
    {
        if (mcpMessage == null)
        {
            return new { error = "Invalid message format" };
        }

        _logger.LogInformation("Processing MCP method: {Method}", mcpMessage.Method);

        try
        {
            return mcpMessage.Method?.ToLower() switch
            {
                "initialize" => HandleInitialize(mcpMessage),
                "initialized" => HandleInitialized(),
                "notifications/initialized" => HandleInitialized(),
                "ping" => HandlePing(mcpMessage),
                "tools/list" => HandleToolsList(mcpMessage),
                "tools/call" => HandleToolsCall(mcpMessage),
                "prompts/list" => HandlePromptsList(mcpMessage),
                "prompts/get" => HandlePromptsGet(mcpMessage),
                "resources/list" => HandleResourcesList(mcpMessage),
                "resources/read" => HandleResourcesRead(mcpMessage),
                "resources/templates/list" => HandleResourceTemplatesList(mcpMessage),
                "completion/complete" => HandleCompletionComplete(mcpMessage),
                _ => new {
                    jsonrpc = "2.0",
                    id = mcpMessage.Id,
                    error = new {
                        code = -32601,
                        message = $"Method not found: {mcpMessage.Method}"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MCP message");
            return new {
                jsonrpc = "2.0",
                id = mcpMessage.Id,
                error = new {
                    code = -32603,
                    message = $"Internal error: {ex.Message}"
                }
            };
        }
    }

    private object HandleInitialize(MCPMessage mcpMessage)
    {
        _logger.LogInformation("Handling initialize request - sending capabilities");
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = new {
                protocolVersion = "2024-11-05",
                capabilities = new {
                    tools = new {
                        listChanged = false
                    },
                    prompts = new {
                        listChanged = false
                    },
                    resources = new {
                        subscribe = false,
                        listChanged = false
                    }
                },
                serverInfo = new {
                    name = "Custom Streamable HTTP MCP server",
                    version = "1.0.0"
                }
            }
        };
    }

    private object? HandleInitialized()
    {
        _logger.LogInformation("Received initialized notification - MCP session is now fully established!");
        return null; // Notification - no response required
    }

    private object HandleToolsList(MCPMessage mcpMessage)
    {
        var result = _toolsService.ListTools();
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = result
        };
    }

    private object HandleToolsCall(MCPMessage mcpMessage)
    {
        var result = _toolsService.CallTool(mcpMessage.Params);
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = result
        };
    }

    private object HandlePromptsList(MCPMessage mcpMessage)
    {
        var result = _promptsService.ListPrompts(mcpMessage.Params);
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = result
        };
    }

    private object HandlePromptsGet(MCPMessage mcpMessage)
    {
        var result = _promptsService.GetPrompt(mcpMessage.Params);
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = result
        };
    }

    private object HandleResourcesList(MCPMessage mcpMessage)
    {
        var result = _resourcesService.ListResources(mcpMessage.Params);
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = result
        };
    }

    private object HandleResourcesRead(MCPMessage mcpMessage)
    {
        var result = _resourcesService.ReadResource(mcpMessage.Params);
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = result
        };
    }

    private object HandlePing(MCPMessage mcpMessage)
    {
        _logger.LogInformation("Handling ping request");
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = new { }
        };
    }

    private object HandleResourceTemplatesList(MCPMessage mcpMessage)
    {
        _logger.LogInformation("Handling resources/templates/list request");
        var result = _resourcesService.ListResourceTemplates(mcpMessage.Params);
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = result
        };
    }

    private object HandleCompletionComplete(MCPMessage mcpMessage)
    {
        _logger.LogInformation("Handling completion/complete request");
        // Return empty completion result - this is an optional/advanced feature
        return new {
            jsonrpc = "2.0",
            id = mcpMessage.Id,
            result = new {
                completion = new {
                    values = new object[0],
                    total = 0,
                    hasMore = false
                }
            }
        };
    }
}