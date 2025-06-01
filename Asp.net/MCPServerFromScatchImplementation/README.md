# MCP Streamable HTTP Server - Model Context Protocol over Streamable HTTP

This ASP.NET Core Web API project implements a Model Context Protocol (MCP) server using Streamable HTTP transport. It provides a standards-compliant MCP server that can be connected to using MCP clients and tools like the MCP Inspector.

## Features

- **MCP Streamable HTTP Server**: Full implementation of MCP protocol over Streamable HTTP transport (2025-03-26 specification)
- **SSE Transport Support**: Server-Sent Events (SSE) backwards compatibility for MCP 2024-11-05 specification
- **Multiple Transport Options**: Both Streamable HTTP and SSE transports available
- **Tool Support**: Exposes tools (HelloTool, GreetUser, EchoTool) that can be called by MCP clients
- **MCP Inspector Compatible**: Can be connected to using the official MCP Inspector
- **JSON-RPC 2.0**: Implements proper MCP message format with id, result, and error fields
- **CORS Support**: Configured for cross-origin requests
- **Backward Compatibility**: Maintains legacy endpoints for testing

## Available Endpoints & URLs

### Primary MCP Endpoints

#### 1. Streamable HTTP Endpoint (Primary - Modern)
- **URL**: `POST /mcp`
- **Description**: Main MCP protocol endpoint supporting all MCP operations
- **Content-Type**: `application/json`
- **Transport**: Streamable HTTP (2025-03-26 specification)
- **Use Case**: Primary endpoint for MCP clients and MCP Inspector

#### 2. Server-Sent Events (SSE) Endpoints (Backwards Compatibility)
- **SSE Connection URL**: `GET /mcp/sse`
  - **Description**: Establishes SSE connection and sends endpoint URI
  - **Returns**: `endpoint` event with POST URI for sending messages
  - **Content-Type**: `text/event-stream`
  
- **SSE Message URL**: `POST /mcp/sse/{connectionId}`
  - **Description**: Receives MCP messages for specific SSE connection
  - **Content-Type**: `application/json`
  - **Use Case**: SSE clients send messages here, responses come via SSE events

**Supported MCP Methods** (both transports):
- `initialize`: Returns server capabilities and info
- `tools/list`: Returns available tools list
- `tools/call`: Execute specific tools
- `prompts/list`: Returns available prompts
- `prompts/get`: Get specific prompt
- `resources/list`: Returns available resources
- `resources/read`: Read specific resource
- `resources/templates/list`: List resource templates

### MCP Inspector Connection

To connect with MCP Inspector:
1. Set **Transport Type**: `Streamable HTTP`
2. Set **URL**: `http://localhost:5253/mcp`
3. Click **Connect**

The server will respond to initialization requests and provide the HelloTool in the tools list.

## MCP Messages

### Initialize Request
```json
{
  "method": "initialize",
  "id": "1"
}
```

**Response**:
```json
{
  "id": "1",
  "result": {
    "capabilities": {
      "tools": {
        "listChanged": false
      }
    },
    "serverInfo": {
      "name": "Custom Streamable HTTP MCP server",
      "version": "1.0.0"
    }
  }
}
```

### Tools List Request
```json
{
  "method": "tools/list",
  "params": {},
  "id": "2"
}
```

**Response**:
```json
{
  "id": "2",
  "result": {
    "tools": [
      {
        "name": "HelloTool",
        "description": "A simple greeting tool that says hello",
        "inputSchema": {
          "type": "object",
          "properties": {},
          "required": []
        }
      },
      {
        "name": "GreetUser",
        "description": "A personalized greeting tool that greets a specific user",
        "inputSchema": {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "description": "The name of the user to greet"
            },
            "title": {
              "type": "string",
              "description": "Optional title for the user (Mr., Ms., Dr., etc.)"
            }
          },
          "required": ["name"]
        }
      },
      {
        "name": "EchoTool",
        "description": "Echoes the message back to the client",
        "inputSchema": {
          "type": "object",
          "properties": {
            "message": {
              "type": "string",
              "description": "The message to echo back"
            }
          },
          "required": ["message"]
        }
      }
    ]
  }
}
```

## Complete URL Reference

### Base URL
When running locally: `http://localhost:5253`

### MCP Protocol Endpoints
| Endpoint | Method | Description | Transport |
|----------|--------|-------------|-----------|
| `/mcp` | POST | Primary MCP endpoint (all operations) | Streamable HTTP |
| `/mcp/sse` | GET | SSE connection establishment | Server-Sent Events |
| `/mcp/sse/{connectionId}` | POST | SSE message sending | Server-Sent Events |

### Legacy Endpoints (Backward Compatibility)
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/send-event` | POST | Legacy event testing endpoint (non-MCP) |
| `/health` | GET | API health status |
| `/` | GET | Redirects to static test page |

### Static Files
| Endpoint | Description |
|----------|-------------|
| `/index.html` | Test client web page |
| `/wwwroot/*` | Static web assets |

## Transport Usage Guide

### For Modern MCP Clients (Recommended)
Use the **Streamable HTTP** transport:
- **URL**: `http://localhost:5253/mcp`
- **Method**: POST for all operations
- **Content-Type**: `application/json`

### For Legacy SSE Clients
Use the **Server-Sent Events** transport:
1. Connect to: `GET http://localhost:5253/mcp/sse`
2. Receive endpoint URI via SSE `endpoint` event
3. Send messages to: `POST http://localhost:5253{received-endpoint}`
4. Receive responses via SSE `message` events

## Running the Application

1. **Build and Run**:
```bash
dotnet run
```

2. **Server URL**: `http://localhost:5253`

3. **Connect with MCP Inspector**:
   - Transport Type: `Streamable HTTP`
   - URL: `http://localhost:5253/mcp`

## Architecture

The server implements:
- **MCPMessageProcessor**: Core MCP message routing and JSON-RPC 2.0 processing
- **MCPToolsService**: Service for managing and listing available tools (HelloTool, GreetUser, EchoTool)
- **SSEConnectionManager**: Manages Server-Sent Events connections and message routing
- **Dual Transport Support**: Both Streamable HTTP (2025-03-26) and SSE (2024-11-05) transports
- **Dependency Injection**: Full ASP.NET Core DI support with scoped services
- **CORS Configuration**: Proper cross-origin support for web clients

## Development Notes

- **Built with .NET 8.0**: Latest framework with modern C# features
- **Dual MCP Transport Support**:
  - Streamable HTTP transport (2025-03-26 specification) - Primary
  - Server-Sent Events transport (2024-11-05 specification) - Backwards compatibility
- **Custom Implementation**: Not using official C# MCP SDK for maximum flexibility
- **SSE Package**: Uses `System.Net.ServerSentEvents 9.0.0` for SSE support
- **Hot Reload Enabled**: Development with `dotnet watch run` for automatic reloading
- **CORS Enabled**: Configured for development and cross-origin requests
- **JSON-RPC 2.0 Compliant**: Proper error handling and response formatting
- **Comprehensive Logging**: Detailed logging for both transports and debugging

## Adding New Tools

To add new tools, extend the `MCPToolsService.ListTools()` method:

```csharp
public object ListTools()
{
    return new
    {
        tools = new[]
        {
            new
            {
                name = "HelloTool",
                description = "HelloToolDescription",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                }
            },
            // Add more tools here
        }
    };
}
```

## Testing with curl

### Streamable HTTP Transport (Primary)

#### MCP Initialize
```bash
curl -X POST http://localhost:5253/mcp \
  -H "Content-Type: application/json" \
  -d '{"method":"initialize","id":"1"}'
```

#### MCP Tools List
```bash
curl -X POST http://localhost:5253/mcp \
  -H "Content-Type: application/json" \
  -d '{"method":"tools/list","params":{},"id":"2"}'
```

#### Call EchoTool
```bash
curl -X POST http://localhost:5253/mcp \
  -H "Content-Type: application/json" \
  -d '{"method":"tools/call","id":"3","params":{"name":"EchoTool","arguments":{"message":"Hello World!"}}}'
```

### Server-Sent Events (SSE) Transport

#### Test SSE Connection
```bash
# In one terminal - establish SSE connection and see endpoint
curl -N http://localhost:5253/mcp/sse

# In another terminal - send message (replace connectionId with actual ID from SSE)
curl -X POST http://localhost:5253/mcp/sse/{connectionId} \
  -H "Content-Type: application/json" \
  -d '{"method":"initialize","id":"1"}'
```

### Legacy Endpoints

#### Health Check
```bash
curl http://localhost:5253/health
```

#### Send Event (Legacy)
```bash
curl -X POST http://localhost:5253/send-event \
  -H "Content-Type: application/json" \
  -d '{"Method":"test","Data":"sample data"}'
```

## Client Examples

### SSE Client (C#)
A complete SSE client example is available in the `../sse-client/` directory:
```bash
cd ../sse-client
dotnet run
```

This client demonstrates:
- SSE connection establishment
- Receiving endpoint URI
- Sending MCP messages via POST
- Receiving responses via SSE events
- Complete tool calling workflow

### Regular HTTP Client
Use the existing `../mcp-client/` for standard HTTP transport testing.

## Project Structure

- **Extensions/**: Extension methods for clean code organization
  - `EndpointExtensions.cs`: MCP, SSE, and legacy endpoint configuration
  - `ServiceExtensions.cs`: Dependency injection configuration
- **Models/**: Data models for MCP protocol
  - `MCPMessage.cs`: Core MCP message structure
  - `ToolModels.cs`: Tool calling parameter models
  - `EventMessage.cs`: Legacy event message model
- **Services/**: Business logic services for MCP operations
  - `MCPMessageProcessor.cs`: Core MCP message routing and processing
  - `MCPToolsService.cs`: Tool management and execution
  - `MCPPromptsService.cs`: Prompt management
  - `MCPResourcesService.cs`: Resource management
  - `SSEConnectionManager.cs`: SSE connection management
  - `LegacyEventProcessor.cs`: Legacy event processing
- **wwwroot/**: Static web files including test client

## Code Quality Features

- **Clean Architecture**: Separated concerns with extensions and services
- **Proper Logging**: Structured logging throughout the application
- **Error Handling**: Comprehensive error handling with proper JSON-RPC responses
- **Type Safety**: Nullable reference types enabled for better code safety