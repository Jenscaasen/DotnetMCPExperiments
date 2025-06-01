# Azure Functions MCP Server

A Model Context Protocol (MCP) server implementation using Azure Functions with the isolated worker model and streamable HTTP transport. This implementation provides a serverless, scalable MCP server that can be deployed to Azure or run locally.

## 🏗️ Architecture

This implementation uses:
- **Azure Functions V4** with isolated worker model (.NET 8.0)
- **Streamable HTTP transport** (MCP 2025-03-26 specification)
- **Dependency Injection** for clean service architecture
- **JSON-RPC 2.0** protocol compliance
- **CORS support** for cross-origin requests

### Key Components

- [`McpStreamEndpoint.cs`](./Functions/McpStreamEndpoint.cs) - HTTP trigger functions for MCP endpoints
- [`MCPMessageProcessor.cs`](./Services/MCPMessageProcessor.cs) - Core message routing and processing
- [`MCPToolsService.cs`](./Services/MCPToolsService.cs) - Tool management and execution
- [`MCPPromptsService.cs`](./Services/MCPPromptsService.cs) - Prompt templates and processing
- [`MCPResourcesService.cs`](./Services/MCPResourcesService.cs) - Resource management

## ⚡ Features

- **Full MCP Protocol Support**: Initialize, tools, prompts, resources, completion
- **Streamable HTTP**: Modern single-endpoint communication pattern
- **Streaming Capabilities**: Demonstrates chunked response streaming
- **Health Monitoring**: Built-in health check endpoint
- **CORS Enabled**: Ready for web client integration
- **Azure Integration**: Application Insights, Functions runtime v4
- **Local Development**: Azure Functions Core Tools support

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#install-the-azure-functions-core-tools)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (for deployment)

### Local Development

1. **Clone and navigate to the project**:
   ```bash
   cd azurefunctions-mcp
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Start the Functions runtime**:
   ```bash
   func start
   ```

4. **Test the server**:
   ```bash
   # Health check
   curl http://localhost:7071/api/health
   
   # MCP Initialize
   curl -X POST http://localhost:7071/api/mcp \
     -H "Content-Type: application/json" \
     -d '{"method":"initialize","id":"1"}'
   ```

## 📡 API Endpoints

### Primary MCP Endpoint

**`POST /api/mcp`** - Main MCP protocol endpoint
- Handles all MCP JSON-RPC 2.0 messages
- Supports initialization, tool calls, prompts, resources
- Returns structured JSON responses

### Supporting Endpoints

**`OPTIONS /api/mcp`** - CORS preflight handler
- Handles browser preflight requests
- Returns appropriate CORS headers

**`POST /api/mcp-stream`** - Streaming demonstration
- Shows chunked response streaming capabilities
- Useful for long-running operations

**`GET /api/health`** - Health check
- Returns server health status
- No authentication required

## 🔧 MCP Protocol Support

### Supported Methods

| Method | Description | Status |
|--------|-------------|--------|
| `initialize` | Server initialization and capabilities | ✅ |
| `initialized` | Initialization completion notification | ✅ |
| `ping` | Connection keep-alive | ✅ |
| `tools/list` | List available tools | ✅ |
| `tools/call` | Execute a tool | ✅ |
| `prompts/list` | List available prompts | ✅ |
| `prompts/get` | Get a specific prompt | ✅ |
| `resources/list` | List available resources | ✅ |
| `resources/read` | Read a specific resource | ✅ |
| `resources/templates/list` | List resource templates | ✅ |
| `completion/complete` | Auto-completion support | ✅ |

### Server Capabilities

```json
{
  "capabilities": {
    "tools": { "listChanged": false },
    "prompts": { "listChanged": false },
    "resources": { 
      "subscribe": false, 
      "listChanged": false 
    }
  },
  "serverInfo": {
    "name": "Custom Streamable HTTP MCP server",
    "version": "1.0.0"
  }
}
```

## 🛠️ Built-in Tools

### GreetUser Tool
- **Purpose**: Demonstrates tool execution with parameters
- **Parameters**: `name` (string), `title` (string, optional)
- **Example**:
  ```json
  {
    "method": "tools/call",
    "params": {
      "name": "GreetUser",
      "arguments": {
        "name": "Alice",
        "title": "Developer"
      }
    },
    "id": "call-1"
  }
  ```

### EchoTool
- **Purpose**: Simple echo functionality for testing
- **Parameters**: `message` (string)
- **Returns**: Echoed message with metadata

## 📝 Configuration

### Host Configuration ([`host.json`](./host.json))

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true
    }
  }
}
```

### Local Settings ([`local.settings.json`](./local.settings.json))

Create this file for local development:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

## 🧪 Testing

### Using curl

1. **Initialize MCP session**:
   ```bash
   curl -X POST http://localhost:7071/api/mcp \
     -H "Content-Type: application/json" \
     -d '{"method":"initialize","id":"1"}'
   ```

2. **List available tools**:
   ```bash
   curl -X POST http://localhost:7071/api/mcp \
     -H "Content-Type: application/json" \
     -d '{"method":"tools/list","id":"2"}'
   ```

3. **Call a tool**:
   ```bash
   curl -X POST http://localhost:7071/api/mcp \
     -H "Content-Type: application/json" \
     -d '{
       "method":"tools/call",
       "params":{
         "name":"GreetUser",
         "arguments":{"name":"World","title":"User"}
       },
       "id":"3"
     }'
   ```

### Using MCP Inspector

1. Install MCP Inspector: `npm install -g @modelcontextprotocol/inspector`
2. Run: `mcp-inspector`
3. Connect with:
   - **Transport**: Streamable HTTP
   - **URL**: `http://localhost:7071/api/mcp`

## 📦 Dependencies

Key NuGet packages:
- `Microsoft.Azure.Functions.Worker` (2.0.0)
- `Microsoft.Azure.Functions.Worker.Extensions.Http` (3.3.0)
- `Microsoft.ApplicationInsights.WorkerService` (2.23.0)

## 🔄 Comparison with ASP.NET Core Implementation

| Aspect | Azure Functions | ASP.NET Core |
|--------|----------------|--------------|
| **Hosting** | Serverless/Consumption | Always-on web server |
| **Scalability** | Automatic scaling | Manual scaling configuration |
| **Cold Start** | Yes (1-3 seconds) | No |
| **Cost Model** | Pay-per-execution | Pay-per-instance |
| **Development** | Function-based | Controller-based |
| **Deployment** | Function App | App Service/Container |
| **Debugging** | Azure Functions Tools | Standard web debugging |
| **State Management** | Stateless by design | Can maintain state |

## 🏃 When to Use Azure Functions

**Choose Azure Functions when**:
- You need automatic scaling from zero
- You have sporadic or unpredictable traffic
- You want to minimize infrastructure management
- You prefer pay-per-use pricing
- You're building event-driven architectures

**Choose ASP.NET Core when**:
- You need consistent low latency
- You have steady, predictable traffic
- You need complex routing or middleware
- You want full control over the hosting environment
- You're building a traditional web API

## 🚀 Deployment

See [`DEPLOYMENT.md`](./DEPLOYMENT.md) for detailed Azure deployment instructions.

## 🔍 Monitoring and Troubleshooting

### Application Insights Integration

The server includes Application Insights integration for:
- Request tracking and performance metrics
- Exception logging and error rates
- Custom telemetry and diagnostics
- Live metrics streaming

### Common Issues

1. **Cold Start Delays**: First request may take 1-3 seconds
2. **CORS Issues**: Ensure proper headers are configured
3. **JSON Parsing**: Verify Content-Type headers are set correctly
4. **Authentication**: Function-level auth requires `?code=xxx` parameter

### Debugging

Enable detailed logging in [`host.json`](./host.json):
```json
{
  "logging": {
    "logLevel": {
      "default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## 📚 Additional Resources

- [Azure Functions Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [.NET Isolated Worker Guide](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [ASP.NET Core Implementation](../aspnetapisse/AspNetApiSse/)