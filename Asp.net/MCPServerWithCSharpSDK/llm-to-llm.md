# ASP.NET Core MCP Server - LLM Implementation Guide

This guide explains how to create a minimal Model Context Protocol (MCP) server using ASP.NET Core from scratch.

## Prerequisites

- .NET 9.0 SDK or later
- Basic understanding of ASP.NET Core

## Project Setup

### 1. Create a new ASP.NET Core Web API project

```bash
dotnet new webapi -n YourMcpServer
cd YourMcpServer
```

### 2. Add Required NuGet Packages

```bash
dotnet add package ModelContextProtocol --version 0.2.0-preview.2
dotnet add package ModelContextProtocol.AspNetCore --version 0.2.0-preview.2
```

### 3. Update the Project File

Your `.csproj` file should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.2.0-preview.2" />
    <PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.2.0-preview.2" />
  </ItemGroup>
</Project>
```

## Minimal MVP Implementation

### Complete Program.cs

Replace the entire contents of `Program.cs` with this minimal implementation:

```csharp
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<EchoTool>();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();

[McpServerToolType]
public sealed class EchoTool
{
    [McpServerTool, Description("Echoes the input back to the client.")]
    public static string Echo(string message)
    {
        return "hello " + message;
    }
}
```

## Key Components Explained

### 1. MCP Server Configuration
- `AddMcpServer()` - Registers MCP server services
- `WithHttpTransport()` - Enables HTTP/SSE transport
- `WithTools<EchoTool>()` - Registers your tool class

### 2. Tool Implementation
- `[McpServerToolType]` - Marks the class as containing MCP tools
- `[McpServerTool]` - Marks the method as an MCP tool
- `Description` - Provides tool description for LLM clients

### 3. Tool Method Requirements
- Must be `public static`
- Can have parameters (will be exposed as tool parameters)
- Return type becomes the tool's output
- Use `[Description]` on parameters to document them

## Running the Server

```bash
dotnet run
```

The server will start on `http://localhost:5000` by default.

The URL `http://localhost:5000` will be the endpoint for the Streamable HTTP,
and `http://localhost:5000/sse` for SSE compatibility in older clients.
 
## Customization Examples

### Adding Parameters to Tools

```csharp
[McpServerTool, Description("Adds two numbers together.")]
public static int Add(
    [Description("First number")] int a, 
    [Description("Second number")] int b)
{
    return a + b;
}
```

### Async Tools with External APIs

```csharp
[McpServerTool, Description("Gets weather information.")]
public static async Task<string> GetWeather(
    [Description("City name")] string city,
    CancellationToken cancellationToken)
{
    using var client = new HttpClient();
    // Your API call logic here
    return $"Weather in {city}: 20°C, sunny";
}
```

### Multiple Tool Classes

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MathTools>()
    .WithTools<WeatherTools>()
    .WithTools<FileTools>();
```

### Using Dependency Injection

```csharp
[McpServerTool, Description("Tool using DI services.")]
public static string UseService(
    ILogger<YourTool> logger,
    [Description("Input message")] string message)
{
    logger.LogInformation("Processing: {Message}", message);
    return $"Processed: {message}";
}
```

## File Structure

The minimal project contains only:
```
YourMcpServer/
├── Program.cs              # Main application and tools
├── YourMcpServer.csproj    # Project configuration
├── appsettings.json        # ASP.NET Core settings (optional)
└── appsettings.Development.json # Development settings (optional)
```

## Testing Your Server

1. Run the server: `dotnet run`
2. The MCP endpoints will be available at the root URL
3. Use an MCP client or tools like Cursor/Claude Desktop to connect
4. Your tools will be automatically discovered and made available

## Common Patterns

### Error Handling
```csharp
[McpServerTool, Description("Tool with error handling.")]
public static string SafeTool(string input)
{
    try
    {
        // Your logic here
        return $"Success: {input}";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message}";
    }
}
```

### Validation
```csharp
[McpServerTool, Description("Tool with validation.")]
public static string ValidatedTool([Description("Must not be empty")] string input)
{
    if (string.IsNullOrWhiteSpace(input))
        throw new ArgumentException("Input cannot be empty");
    
    return $"Valid input: {input}";
}
```

## Advanced Features

### Custom Transport Configuration
```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Port = 3001;
        options.Host = "localhost";
    });
```

### Multiple Tool Methods in One Class
```csharp
[McpServerToolType]
public sealed class UtilityTools
{
    [McpServerTool, Description("Converts text to uppercase.")]
    public static string ToUpper(string text) => text.ToUpper();

    [McpServerTool, Description("Reverses text.")]
    public static string Reverse(string text) => new(text.Reverse().ToArray());
}
```

## Tips for LLM Implementation

1. **Start Minimal**: Begin with the exact MVP above and add features incrementally
2. **Clear Descriptions**: Always provide meaningful descriptions for tools and parameters
3. **Error Handling**: Wrap tool logic in try-catch blocks for better user experience
4. **Static Methods**: Keep tool methods static unless you need dependency injection
5. **Simple Types**: Use basic types (string, int, bool) for parameters when possible
6. **Single Responsibility**: Each tool should do one thing well

This guide provides everything needed to create a functional MCP server that can be customized for any specific use case.
