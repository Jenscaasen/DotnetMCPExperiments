using System.Collections.Concurrent;
using System.Text.Json;

namespace AspNetApiSse.Services;

/// <summary>
/// Manages Server-Sent Events connections for MCP clients
/// </summary>
public class SSEConnectionManager
{
    private readonly ConcurrentDictionary<string, SSEConnection> _connections = new();
    private readonly ILogger<SSEConnectionManager> _logger;

    public SSEConnectionManager(ILogger<SSEConnectionManager> logger)
    {
        _logger = logger;
    }

    public string AddConnection(HttpResponse response, string clientId)
    {
        var connectionId = Guid.NewGuid().ToString();
        var connection = new SSEConnection(connectionId, response, clientId);
        
        _connections[connectionId] = connection;
        _logger.LogInformation("Added SSE connection {ConnectionId} for client {ClientId}", connectionId, clientId);
        
        return connectionId;
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            _logger.LogInformation("Removed SSE connection {ConnectionId}", connectionId);
        }
    }

    public async Task SendEventAsync(string connectionId, string eventType, object data)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            await connection.SendEventAsync(eventType, data);
        }
    }

    public async Task SendToAllAsync(string eventType, object data)
    {
        var tasks = _connections.Values.Select(conn => conn.SendEventAsync(eventType, data));
        await Task.WhenAll(tasks);
    }

    public SSEConnection? GetConnection(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var connection);
        return connection;
    }
}

/// <summary>
/// Represents a single SSE connection
/// </summary>
public class SSEConnection
{
    public string ConnectionId { get; }
    public string ClientId { get; }
    public HttpResponse Response { get; }
    public DateTime CreatedAt { get; }

    public SSEConnection(string connectionId, HttpResponse response, string clientId)
    {
        ConnectionId = connectionId;
        Response = response;
        ClientId = clientId;
        CreatedAt = DateTime.UtcNow;
    }

    public async Task SendEventAsync(string eventType, object data)
    {
        try
        {
            // For strings, send as-is. For objects, serialize as JSON
            var dataContent = data is string str ? str : JsonSerializer.Serialize(data);
            var sseData = $"event: {eventType}\ndata: {dataContent}\n\n";
            
            await Response.WriteAsync(sseData);
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            // Log the error but don't throw - connection might be closed
            Console.WriteLine($"Error sending SSE event: {ex.Message}");
        }
    }
}