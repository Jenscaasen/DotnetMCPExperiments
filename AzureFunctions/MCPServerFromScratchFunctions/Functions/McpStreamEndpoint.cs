using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using McpStreamFunc.Models;
using McpStreamFunc.Services;

namespace McpStreamFunc.Functions;

/// <summary>
/// Azure Functions endpoint for MCP (Model Context Protocol) with streamable HTTP support
/// </summary>
public class McpStreamEndpoint
{
    private readonly ILogger<McpStreamEndpoint> _logger;
    private readonly MCPMessageProcessor _messageProcessor;

    public McpStreamEndpoint(ILogger<McpStreamEndpoint> logger, MCPMessageProcessor messageProcessor)
    {
        _logger = logger;
        _messageProcessor = messageProcessor;
    }

    /// <summary>
    /// Main MCP endpoint supporting JSON-RPC 2.0 over HTTP
    /// </summary>
    [Function("mcp")]
    public async Task<HttpResponseData> McpAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        // Set CORS headers for the response
        var response = req.CreateResponse();
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");

        // Log request details
        _logger.LogInformation("=== MCP Request Details ===");
        _logger.LogInformation("Method: {Method}", req.Method);
        _logger.LogInformation("Content-Type: {ContentType}", req.Headers.GetValues("Content-Type").FirstOrDefault() ?? "null");
        _logger.LogInformation("Headers:");
        foreach (var header in req.Headers)
        {
            _logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
        }

        try
        {
            // Read request body
            using var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();

            _logger.LogInformation("Raw Request Body Length: {Length}", requestBody.Length);
            _logger.LogInformation("Raw Request Body: {Body}", requestBody);

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Request body is empty or whitespace only");
                response.StatusCode = HttpStatusCode.BadRequest;
                
                var errorResponse = new {
                    jsonrpc = "2.0",
                    id = "unknown",
                    error = new {
                        code = -32700,
                        message = "Parse error: Empty request body"
                    }
                };
                
                await response.WriteAsJsonAsync(errorResponse, cancellationToken);
                return response;
            }

            _logger.LogInformation("Attempting to deserialize JSON...");
            var mcpMessage = JsonSerializer.Deserialize<MCPMessage>(requestBody);
            _logger.LogInformation("JSON deserialization successful");
            _logger.LogInformation("Parsed message: method={Method}, id={Id}", mcpMessage?.Method, mcpMessage?.Id);

            var result = _messageProcessor.ProcessMessage(mcpMessage);

            // Handle notifications (which return null and don't need a response)
            if (result == null)
            {
                _logger.LogInformation("Processed notification: {Method}", mcpMessage?.Method);
                response.StatusCode = HttpStatusCode.OK;
                return response; // Return 200 OK with no body for notifications
            }

            _logger.LogInformation("Response generated: {Response}", JsonSerializer.Serialize(result));

            // Return JSON response with proper content type
            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(result, cancellationToken);
            
            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed for request body");
            response.StatusCode = HttpStatusCode.BadRequest;
            
            var errorResponse = new {
                jsonrpc = "2.0",
                id = "unknown",
                error = new {
                    code = -32700,
                    message = $"Parse error: Invalid JSON - {ex.Message}"
                }
            };
            
            await response.WriteAsJsonAsync(errorResponse, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing MCP message");
            response.StatusCode = HttpStatusCode.InternalServerError;
            
            var errorResponse = new {
                jsonrpc = "2.0",
                id = "unknown",
                error = new {
                    code = -32603,
                    message = $"Internal error: {ex.Message}"
                }
            };
            
            await response.WriteAsJsonAsync(errorResponse, cancellationToken);
            return response;
        }
    }

    /// <summary>
    /// Handle preflight OPTIONS requests for CORS
    /// </summary>
    [Function("mcp-options")]
    public HttpResponseData McpOptionsAsync(
        [HttpTrigger(AuthorizationLevel.Function, "options")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        
        _logger.LogInformation("Handled OPTIONS preflight request");
        return response;
    }

    /// <summary>
    /// Example of streaming endpoint (demonstrates streaming capability)
    /// </summary>
    [Function("mcp-stream")]
    public async Task<HttpResponseData> McpStreamAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP streaming endpoint demo");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Access-Control-Allow-Origin", "*");

        // DO NOT set Content-Length, that would disable chunking
        // Start sending chunks demonstrating streaming capability
        for (var i = 0; i < 5; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var chunkObj = new { 
                jsonrpc = "2.0",
                id = i,
                result = new {
                    index = i, 
                    message = $"Streaming chunk {i}",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            };
            
            var chunk = JsonSerializer.Serialize(chunkObj) + "\n";
            await response.Body.WriteAsync(Encoding.UTF8.GetBytes(chunk), cancellationToken);
            await response.Body.FlushAsync(cancellationToken);

            _logger.LogInformation("Sent streaming chunk {Index}", i);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // simulate work
        }

        _logger.LogInformation("Completed MCP streaming demo");
        return response;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [Function("health")]
    public HttpResponseData HealthAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        
        var healthResponse = new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            version = "1.0.0",
            service = "MCP Azure Functions"
        };
        
        response.WriteString(JsonSerializer.Serialize(healthResponse));
        _logger.LogInformation("Health check requested");
        
        return response;
    }
}