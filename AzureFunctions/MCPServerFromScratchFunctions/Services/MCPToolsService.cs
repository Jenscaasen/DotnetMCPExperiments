using System.Text.Json;
using McpStreamFunc.Models;

namespace McpStreamFunc.Services;

/// <summary>
/// Service for handling MCP tool operations
/// </summary>
public class MCPToolsService
{
    public object ListTools()
    {
        return new
        {
            tools = new object[]
            {
                new
                {
                    name = "HelloTool",
                    description = "A simple greeting tool that says hello",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = new string[] { }
                    }
                },
                new
                {
                    name = "GreetUser",
                    description = "A personalized greeting tool that greets a specific user",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new
                            {
                                type = "string",
                                description = "The name of the user to greet"
                            },
                            title = new
                            {
                                type = "string",
                                description = "Optional title for the user (Mr., Ms., Dr., etc.)"
                            }
                        },
                        required = new[] { "name" }
                    }
                }
            }
        };
    }

    public object CallTool(object? toolParams)
    {
        try
        {
            if (toolParams == null)
            {
                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "Error: No tool parameters provided"
                        }
                    }
                };
            }

            // Parse the tool call parameters
            var json = JsonSerializer.Serialize(toolParams);
            var callParams = JsonSerializer.Deserialize<ToolCallParams>(json);

            if (callParams?.Name == null)
            {
                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "Error: Tool name not specified"
                        }
                    }
                };
            }

            return callParams.Name.ToLower() switch
            {
                "hellotool" => new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "Hello! I'm a tool from the MCP server. Nice to meet you!"
                        }
                    }
                },
                "greetuser" => HandleGreetUser(callParams.Arguments),
                _ => new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error: Unknown tool '{callParams.Name}'"
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Error executing tool: {ex.Message}"
                    }
                }
            };
        }
    }

    private object HandleGreetUser(object? arguments)
    {
        try
        {
            if (arguments == null)
            {
                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "Error: No arguments provided for GreetUser tool"
                        }
                    }
                };
            }

            var json = JsonSerializer.Serialize(arguments);
            var greetArgs = JsonSerializer.Deserialize<GreetUserArgs>(json);

            if (string.IsNullOrWhiteSpace(greetArgs?.Name))
            {
                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "Error: Name parameter is required for GreetUser tool"
                        }
                    }
                };
            }

            var greeting = string.IsNullOrWhiteSpace(greetArgs.Title)
                ? $"Hello, {greetArgs.Name}! Welcome to our MCP server!"
                : $"Hello, {greetArgs.Title} {greetArgs.Name}! Welcome to our MCP server!";

            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = greeting
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Error processing GreetUser arguments: {ex.Message}"
                    }
                }
            };
        }
    }
}