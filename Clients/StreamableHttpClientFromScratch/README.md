# MCP Client - C# Console Application

This console application demonstrates how to connect to a Model Context Protocol (MCP) server using Streamable HTTP transport. It connects to our custom MCP server and interacts with its tools.

## Features

- **Streamable HTTP Connection**: Connects to MCP servers via HTTP POST requests
- **JSON-RPC 2.0**: Implements proper MCP protocol message format
- **Tool Discovery**: Lists all available tools from the server
- **Error Handling**: Robust error handling for connection issues

## Prerequisites

- .NET 8.0 or later
- Running MCP server at `http://localhost:5253/mcp`

## Installation

1. **Clone or navigate to the project directory**
2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

## Usage

1. **Ensure the MCP server is running**:
   ```bash
   # In the server directory
   cd ../aspnetapisse/AspNetApiSse
   dotnet run
   ```

2. **Run the client**:
   ```bash
   dotnet run
   ```

## What the Client Does

1. **Initialize Connection**: Sends an `initialize` request to establish the MCP session
2. **List Tools**: Requests the available tools from the server using `tools/list`
3. **Display Results**: Shows the server capabilities and available tools

## Example Output

```
MCP Client - Connecting to Streamable HTTP Server
=================================================
🔗 Connecting to MCP server...
✅ Initialized connection
   Server response: {"id":"1","result":{"capabilities":{"tools":{"listChanged":false}},"serverInfo":{"name":"Custom Streamable HTTP MCP server","version":"1.0.0"}}}

📋 Listing available tools...
✅ Retrieved tools list
   Server response: {"id":"2","result":{"tools":[{"name":"HelloTool","description":"HelloToolDescription","inputSchema":{"type":"object","properties":{},"required":[]}}]}}

Available tools:
  • HelloTool: HelloToolDescription
    Input Schema: {"type":"object","properties":{},"required":[]}

✅ MCP client demo completed successfully!
```

## Implementation Details

### Manual JSON-RPC Implementation

Since we needed maximum flexibility and the C# MCP SDK transport classes weren't immediately accessible, this client implements the MCP protocol manually using:

- **HttpClient**: For HTTP communication
- **JSON-RPC 2.0**: Manual message formatting
- **Streamable HTTP**: Single endpoint POST-based communication

### Message Format

The client sends JSON-RPC 2.0 formatted messages:

```json
{
  "method": "initialize",
  "id": "1",
  "params": {}
}
```

And receives responses in the format:

```json
{
  "id": "1",
  "result": {
    "capabilities": {"tools": {"listChanged": false}},
    "serverInfo": {"name": "Custom Streamable HTTP MCP server", "version": "1.0.0"}
  }
}
```

## Extending the Client

To add more functionality:

1. **Tool Calling**: Add support for calling specific tools
2. **Error Handling**: Enhanced error parsing and handling
3. **Streaming**: Support for streaming responses
4. **Authentication**: Add authentication headers if needed

### Example Tool Call

```csharp
var toolCallRequest = new
{
    method = "tools/call",
    id = "3",
    @params = new
    {
        name = "HelloTool",
        arguments = new { }
    }
};
```

## Dependencies

- **ModelContextProtocol** (0.2.0-preview.2): Official C# MCP SDK (for future full integration)
- **System.Text.Json**: JSON serialization/deserialization

## Architecture

```
┌─────────────────┐    HTTP POST     ┌─────────────────┐
│   MCP Client    │ ──────────────► │   MCP Server    │
│  (Console App)  │                 │ (ASP.NET Core)  │
│                 │ ◄────────────── │                 │
└─────────────────┘   JSON-RPC      └─────────────────┘
     localhost:5253/mcp
```

## Future Enhancements

- Integration with official C# MCP SDK transport classes
- Support for more MCP features (prompts, resources)
- Configuration file support
- Multiple server connections
- Interactive mode for tool calling