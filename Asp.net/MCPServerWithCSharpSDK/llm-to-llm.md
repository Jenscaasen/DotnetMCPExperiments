# ASP.NET Core MCP Server - LLM Implementation Guide

Dear fellow LLM, so the user has asked you to create an MCP server using ASP.NET Core. This guide explains how to create a minimal Model Context Protocol (MCP) server using ASP.NET Core from scratch, including best practices for error handling and authentication.

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

### Proper MCP Error Handling

**IMPORTANT**: Use `McpException` for proper MCP protocol compliance instead of generic exceptions. This ensures meaningful error messages reach the client instead of generic "An error occurred invoking 'ToolName'" messages.

```csharp
using ModelContextProtocol; // Required import

[McpServerTool, Description("Tool with proper MCP error handling.")]
public static string ValidatedTool([Description("Input message")] string input)
{
    // Validation errors - throw McpException directly
    if (string.IsNullOrWhiteSpace(input))
        throw new McpException("Input cannot be empty");
    
    try
    {
        // Your business logic here
        if (input.Length > 100)
            throw new McpException("Input too long - maximum 100 characters allowed");
            
        return $"Success: {input}";
    }
    catch (HttpRequestException ex)
    {
        // Convert system exceptions to MCP exceptions
        throw new McpException($"API error: {ex.Message}");
    }
    catch (JsonException ex)
    {
        throw new McpException($"JSON parsing error: {ex.Message}");
    }
    catch (Exception ex) when (!(ex is McpException))
    {
        // Preserve McpExceptions, convert others
        throw new McpException($"Unexpected error: {ex.Message}");
    }
}
```

**Key Points:**
- Always import `using ModelContextProtocol;`
- Use `throw new McpException("meaningful message")` for all errors
- Preserve existing `McpException` instances with `when (!(ex is McpException))`
- Provide specific, actionable error messages
- Convert system exceptions to `McpException` for consistency

### Legacy Error Handling (Not Recommended)
```csharp
[McpServerTool, Description("Tool with basic error handling.")]
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

## Client Authentication Pattern

When your MCP tools need to make authenticated API calls using credentials provided by the MCP client, implement this pattern:

### 1. Create an Authentication Token Provider

```csharp
public class AuthTokenProvider
{
    private readonly AsyncLocal<string?> _token = new();
    private readonly ILogger<AuthTokenProvider> _logger;

    public AuthTokenProvider(ILogger<AuthTokenProvider> logger)
    {
        _logger = logger;
    }

    public string? GetToken()
    {
        return _token.Value;
    }

    public void SetToken(string token)
    {
        _token.Value = token;
    }
}
```

### 2. Create Middleware to Extract Authorization Headers

```csharp
public class AuthTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthTokenProvider _authTokenProvider;

    public AuthTokenMiddleware(RequestDelegate next, AuthTokenProvider authTokenProvider)
    {
        _next = next;
        _authTokenProvider = authTokenProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract authorization header if present
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!string.IsNullOrEmpty(token))
            {
                _authTokenProvider.SetToken(token);
            }
        }

        await _next(context);
    }
}
```

### 3. Register Services and Middleware

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the auth token provider as singleton
builder.Services.AddSingleton<AuthTokenProvider>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<YourTools>();

var app = builder.Build();

// Add the authentication middleware BEFORE MCP
app.UseMiddleware<AuthTokenMiddleware>();

app.MapMcp("/mcp");
app.Run();
```

### 4. Use Authentication in Tools

```csharp
[McpServerToolType]
public sealed class AuthenticatedTools
{
    private readonly AuthTokenProvider _authTokenProvider;
    private readonly HttpClient _httpClient;

    public AuthenticatedTools(AuthTokenProvider authTokenProvider, HttpClient httpClient)
    {
        _authTokenProvider = authTokenProvider;
        _httpClient = httpClient;
    }

    [McpServerTool, Description("Makes an authenticated API call.")]
    public async Task<string> CallExternalApi([Description("API endpoint")] string endpoint)
    {
        var token = _authTokenProvider.GetToken();
        if (string.IsNullOrEmpty(token))
            throw new McpException("No authorization token found. MCP client must provide a Bearer token for API access.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("Authorization", $"Bearer {token}");
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new McpException($"API error: {ex.Message}");
        }
    }
}
```

**Key Benefits of This Pattern:**
- Works with both SSE and HTTP transports automatically
- Thread-safe using `AsyncLocal<T>`
- Centralizes authentication logic
- Allows tools to access client-provided credentials securely
- Supports any Bearer token-based authentication scheme

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
3. **Proper Error Handling**: Always use `McpException` instead of generic exceptions for meaningful error messages
4. **Authentication Pattern**: Use the AuthTokenProvider pattern when tools need client-provided credentials
5. **Static Methods**: Keep tool methods static unless you need dependency injection
6. **Simple Types**: Use basic types (string, int, bool) for parameters when possible
7. **Single Responsibility**: Each tool should do one thing well
8. **Import Requirements**: Remember to add `using ModelContextProtocol;` for error handling
9. **Using expected url format**: Most clients use `/mcp` and `/mcp/sse` respectively as endpoint addresses. To support this, simply use `app.MapMcp("/mcp");`

This guide provides everything needed to create a functional MCP server that can be customized for any specific use case, with proper error handling and authentication patterns.
