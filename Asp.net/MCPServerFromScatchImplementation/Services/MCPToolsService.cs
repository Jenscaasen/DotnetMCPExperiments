using System.Text.Json;
using AspNetApiSse.Models;

namespace AspNetApiSse.Services;

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
                },
                new
                {
                    name = "EchoTool",
                    description = "Echoes the message back to the client",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            message = new
                            {
                                type = "string",
                                description = "The message to echo back"
                            }
                        },
                        required = new[] { "message" }
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

            if (callParams?.name == null)
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

            return callParams.name.ToLower() switch
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
                "greetuser" => HandleGreetUser(callParams.arguments),
                "echotool" => HandleEchoTool(callParams.arguments),
                _ => new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error: Unknown tool '{callParams.name}'"
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

            if (string.IsNullOrWhiteSpace(greetArgs?.name))
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

            var greeting = string.IsNullOrWhiteSpace(greetArgs.title)
                ? $"Hello, {greetArgs.name}! Welcome to our MCP server!"
                : $"Hello, {greetArgs.title} {greetArgs.name}! Welcome to our MCP server!";

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

    private object HandleEchoTool(object? arguments)
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
                            text = "Error: No arguments provided for EchoTool"
                        }
                    }
                };
            }

            var json = JsonSerializer.Serialize(arguments);
            var echoArgs = JsonSerializer.Deserialize<EchoToolArgs>(json);

            if (string.IsNullOrWhiteSpace(echoArgs?.message))
            {
                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "Error: Message parameter is required for EchoTool"
                        }
                    }
                };
            }

            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Echo: {echoArgs.message}"
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
                        text = $"Error processing EchoTool arguments: {ex.Message}"
                    }
                }
            };
        }
    }
}