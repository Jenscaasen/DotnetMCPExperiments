using System.Text.Json;
using System.Text;
using AspNetApiSse.Models;
using AspNetApiSse.Services;

namespace AspNetApiSse.Extensions;

/// <summary>
/// Extension methods for configuring API endpoints
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Configures MCP endpoints for the application
    /// </summary>
    public static WebApplication ConfigureMCPEndpoints(this WebApplication app)
    {
        // MCP Streamable HTTP endpoint - Single endpoint for all communication
        app.MapPost("/mcp", async (HttpContext context, MCPMessageProcessor messageProcessor, ILogger<Program> logger) =>
        {
            return await HandleMCPRequest(context, messageProcessor, logger);
        })
        .WithName("MCPStreamableHttp")
        .WithOpenApi();

        // Handle preflight OPTIONS requests for CORS
        app.MapMethods("/mcp", new[] { "OPTIONS" }, (HttpContext context) =>
        {
            SetCorsHeaders(context);
            return Results.Ok();
        })
        .WithName("MCPOptions")
        .WithOpenApi();

        // SSE endpoint for MCP - Establish SSE connection and send endpoint info
        app.MapGet("/mcp/sse", async (HttpContext context, SSEConnectionManager connectionManager, ILogger<Program> logger) =>
        {
            await HandleSSEConnection(context, connectionManager, logger);
        })
        .WithName("MCPServerSentEvents")
        .WithOpenApi();

        // SSE POST endpoint for receiving messages from clients
        app.MapPost("/mcp/sse/{connectionId}", async (string connectionId, HttpContext context, MCPMessageProcessor messageProcessor, SSEConnectionManager connectionManager, ILogger<Program> logger) =>
        {
            return await HandleSSEMessage(connectionId, context, messageProcessor, connectionManager, logger);
        })
        .WithName("MCPSSEMessages")
        .WithOpenApi();

        // Temporary debugging - add catch-all route to see what URL is being hit
        app.Map("/{**path}", async (HttpContext context, ILogger<Program> logger) =>
        {
            logger.LogWarning("=== UNMATCHED REQUEST ===");
            logger.LogWarning("Method: {Method}", context.Request.Method);
            logger.LogWarning("Path: {Path}", context.Request.Path);
            logger.LogWarning("Query: {Query}", context.Request.QueryString);
            return Results.NotFound(new { error = "Endpoint not found", path = context.Request.Path.ToString() });
        });

        return app;
    }

    /// <summary>
    /// Configures legacy endpoints for backward compatibility
    /// </summary>
    public static WebApplication ConfigureLegacyEndpoints(this WebApplication app)
    {
        // Endpoint to send events to connected clients (backward compatibility)
        app.MapPost("/send-event", (EventMessage eventMessage, LegacyEventProcessor eventProcessor, HttpContext context) =>
        {
            // In a real application, you would store connections and broadcast to them
            // For this demo, we'll just return the processed event
            
            var processedEvent = eventProcessor.ProcessEvent(eventMessage);
            
            return Results.Ok(new {
                received = eventMessage,
                processed = processedEvent,
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("SendEvent")
        .WithOpenApi();

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithName("HealthCheck")
        .WithOpenApi();

        // Default route to serve the test client
        app.MapGet("/", () => Results.Redirect("/index.html"))
        .WithName("Home")
        .WithOpenApi();

        return app;
    }

    private static async Task<IResult> HandleMCPRequest(HttpContext context, MCPMessageProcessor messageProcessor, ILogger<Program> logger)
    {
        // Set CORS headers
        SetCorsHeaders(context);
        
        // Log request details
        LogRequestDetails(context, logger);
        
        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        
        logger.LogInformation("Raw Request Body Length: {Length}", requestBody.Length);
        logger.LogInformation("Raw Request Body: {Body}", requestBody);
        logger.LogInformation("Request Body (hex): {Hex}", Convert.ToHexString(Encoding.UTF8.GetBytes(requestBody)));
        
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            logger.LogWarning("Request body is empty or whitespace only");
            context.Response.ContentType = "application/json";
            return Results.BadRequest(new {
                id = "unknown",
                error = new {
                    code = -32700,
                    message = "Parse error: Empty request body"
                }
            });
        }
        
        try
        {
            logger.LogInformation("Attempting to deserialize JSON...");
            var mcpMessage = JsonSerializer.Deserialize<MCPMessage>(requestBody);
            logger.LogInformation("JSON deserialization successful");
            logger.LogInformation("Parsed message: method={Method}, id={Id}", mcpMessage?.Method, mcpMessage?.Id);
            
            var response = messageProcessor.ProcessMessage(mcpMessage);
            
            // Handle notifications (which return null and don't need a response)
            if (response == null)
            {
                logger.LogInformation("Processed notification: {Method}", mcpMessage?.Method);
                return Results.Ok(); // Return 200 OK with no body for notifications
            }
            
            logger.LogInformation("Response generated: {Response}", JsonSerializer.Serialize(response));
            
            // Return JSON response with proper content type
            context.Response.ContentType = "application/json";
            return Results.Ok(response);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON deserialization failed for request body: {Body}", requestBody);
            context.Response.ContentType = "application/json";
            return Results.BadRequest(new {
                id = "unknown",
                error = new {
                    code = -32700,
                    message = $"Parse error: Invalid JSON - {ex.Message}"
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing MCP message");
            context.Response.ContentType = "application/json";
            return Results.Problem($"Error processing MCP message: {ex.Message}");
        }
    }

    private static void SetCorsHeaders(HttpContext context)
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        context.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
    }

    private static void LogRequestDetails(HttpContext context, ILogger<Program> logger)
    {
        logger.LogInformation("=== MCP Request Details ===");
        logger.LogInformation("Method: {Method}", context.Request.Method);
        logger.LogInformation("Content-Type: {ContentType}", context.Request.ContentType ?? "null");
        logger.LogInformation("Content-Length: {ContentLength}", context.Request.ContentLength?.ToString() ?? "null");
        logger.LogInformation("Headers:");
        foreach (var header in context.Request.Headers)
        {
            logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value.ToArray()));
        }
    }

    private static async Task HandleSSEConnection(HttpContext context, SSEConnectionManager connectionManager, ILogger<Program> logger)
    {
        // Set SSE headers
        context.Response.Headers["Content-Type"] = "text/event-stream";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";
        SetCorsHeaders(context);

        logger.LogInformation("=== SSE Connection Request ===");
        logger.LogInformation("Client IP: {ClientIP}", context.Connection.RemoteIpAddress);
        
        // Generate a unique client ID
        var clientId = context.Request.Query["clientId"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        
        // Add the connection to the manager
        var connectionId = connectionManager.AddConnection(context.Response, clientId);
        
        // Send the endpoint event as per MCP specification
        var endpointUri = $"/mcp/sse/{connectionId}";
        await connectionManager.SendEventAsync(connectionId, "endpoint", endpointUri);
        
        logger.LogInformation("SSE connection established: {ConnectionId}, Endpoint: {Endpoint}", connectionId, endpointUri);
        
        // Keep the connection alive
        try
        {
            var cancellationToken = context.RequestAborted;
            while (!cancellationToken.IsCancellationRequested)
            {
                // Send a ping every 30 seconds to keep connection alive
                await Task.Delay(30000, cancellationToken);
                await connectionManager.SendEventAsync(connectionId, "ping", new { timestamp = DateTime.UtcNow });
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
            logger.LogInformation("SSE client disconnected: {ConnectionId}", connectionId);
        }
        finally
        {
            connectionManager.RemoveConnection(connectionId);
        }

        // SSE connection ends here - no return value needed as response is streaming
    }

    private static async Task<IResult> HandleSSEMessage(string connectionId, HttpContext context, MCPMessageProcessor messageProcessor, SSEConnectionManager connectionManager, ILogger<Program> logger)
    {
        // Set CORS headers
        SetCorsHeaders(context);
        
        logger.LogInformation("=== SSE Message Request ===");
        logger.LogInformation("Connection ID: {ConnectionId}", connectionId);
        
        // Verify the connection exists
        var connection = connectionManager.GetConnection(connectionId);
        if (connection == null)
        {
            logger.LogWarning("SSE connection not found: {ConnectionId}", connectionId);
            return Results.NotFound(new { error = "SSE connection not found" });
        }

        // Read and process the message like the regular MCP endpoint
        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        
        logger.LogInformation("SSE Message Body: {Body}", requestBody);
        
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            logger.LogWarning("Empty request body in SSE message");
            await connectionManager.SendEventAsync(connectionId, "message", new {
                jsonrpc = "2.0",
                id = "unknown",
                error = new {
                    code = -32700,
                    message = "Parse error: Empty request body"
                }
            });
            return Results.Ok();
        }
        
        try
        {
            var mcpMessage = System.Text.Json.JsonSerializer.Deserialize<MCPMessage>(requestBody);
            logger.LogInformation("SSE Parsed message: method={Method}, id={Id}", mcpMessage?.Method, mcpMessage?.Id);
            
            var response = messageProcessor.ProcessMessage(mcpMessage);
            
            // Handle notifications (which return null and don't need a response)
            if (response == null)
            {
                logger.LogInformation("SSE Processed notification: {Method}", mcpMessage?.Method);
                return Results.Ok(); // Return 200 OK with no body for notifications
            }
            
            // Send the response back via SSE
            await connectionManager.SendEventAsync(connectionId, "message", response);
            logger.LogInformation("SSE Response sent via SSE: {Response}", System.Text.Json.JsonSerializer.Serialize(response));
            
            return Results.Ok();
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogError(ex, "SSE JSON deserialization failed for request body: {Body}", requestBody);
            await connectionManager.SendEventAsync(connectionId, "message", new {
                jsonrpc = "2.0",
                id = "unknown",
                error = new {
                    code = -32700,
                    message = $"Parse error: Invalid JSON - {ex.Message}"
                }
            });
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SSE Unexpected error processing MCP message");
            await connectionManager.SendEventAsync(connectionId, "message", new {
                jsonrpc = "2.0",
                id = "unknown",
                error = new {
                    code = -32603,
                    message = $"Internal error: {ex.Message}"
                }
            });
            return Results.Ok();
        }
    }
}