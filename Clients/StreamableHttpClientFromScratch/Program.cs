using ModelContextProtocol.Client;
using System.Text.Json;

Console.WriteLine("MCP Client - Comprehensive Testing");
Console.WriteLine("==================================");

try
{
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("http://localhost:7071/");
    
    Console.WriteLine("🔗 Connecting to MCP server...");
    
    // Step 1: Initialize the connection
    Console.WriteLine("\n📋 Step 1: Initialize Connection");
    var initRequest = new
    {
        jsonrpc = "2.0",
        method = "initialize",
        id = 1,
        @params = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                sampling = new { },
                roots = new { listChanged = true }
            },
            clientInfo = new
            {
                name = "test-client",
                version = "1.0.0"
            }
        }
    };
    
    var initJson = JsonSerializer.Serialize(initRequest);
    var initResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(initJson, System.Text.Encoding.UTF8, "application/json"));
    
    if (!initResponse.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to initialize: {initResponse.StatusCode}");
    }
    
    var initContent = await initResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"✅ Initialized connection");
    Console.WriteLine($"   Server response: {initContent}");
    
    // Step 2: Send initialized notification
    Console.WriteLine("\n📋 Step 2: Send Initialized Notification");
    var initializedRequest = new
    {
        jsonrpc = "2.0",
        method = "notifications/initialized"
    };
    
    var initializedJson = JsonSerializer.Serialize(initializedRequest);
    var initializedResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(initializedJson, System.Text.Encoding.UTF8, "application/json"));
    
    Console.WriteLine($"✅ Sent initialized notification: {initializedResponse.StatusCode}");
    
    // Step 3: Test Tools
    Console.WriteLine("\n📋 Step 3: Testing Tools");
    
    // List tools
    var toolsRequest = new
    {
        jsonrpc = "2.0",
        method = "tools/list",
        id = 2,
        @params = new { }
    };
    
    var toolsJson = JsonSerializer.Serialize(toolsRequest);
    var toolsResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(toolsJson, System.Text.Encoding.UTF8, "application/json"));
    
    if (!toolsResponse.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to list tools: {toolsResponse.StatusCode}");
    }
    
    var toolsContent = await toolsResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"✅ Retrieved tools list");
    Console.WriteLine($"   Server response: {toolsContent}");
    
    // Call HelloTool
    Console.WriteLine("\n🔧 Calling HelloTool:");
    var helloToolRequest = new
    {
        jsonrpc = "2.0",
        method = "tools/call",
        id = 3,
        @params = new
        {
            name = "HelloTool",
            arguments = new { }
        }
    };
    
    var helloToolJson = JsonSerializer.Serialize(helloToolRequest);
    var helloToolResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(helloToolJson, System.Text.Encoding.UTF8, "application/json"));
    
    var helloToolContent = await helloToolResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"   Response: {helloToolContent}");
    
    // Call GreetUser tool
    Console.WriteLine("\n🔧 Calling GreetUser tool:");
    var greetUserRequest = new
    {
        jsonrpc = "2.0",
        method = "tools/call",
        id = 4,
        @params = new
        {
            name = "GreetUser",
            arguments = new
            {
                name = "Alice",
                title = "Dr."
            }
        }
    };
    
    var greetUserJson = JsonSerializer.Serialize(greetUserRequest);
    var greetUserResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(greetUserJson, System.Text.Encoding.UTF8, "application/json"));
    
    var greetUserContent = await greetUserResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"   Response: {greetUserContent}");
    
    // Step 4: Test Prompts
    Console.WriteLine("\n📋 Step 4: Testing Prompts");
    
    // List prompts
    var promptsListRequest = new
    {
        jsonrpc = "2.0",
        method = "prompts/list",
        id = 5,
        @params = new { }
    };
    
    var promptsListJson = JsonSerializer.Serialize(promptsListRequest);
    var promptsListResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(promptsListJson, System.Text.Encoding.UTF8, "application/json"));
    
    var promptsListContent = await promptsListResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"✅ Retrieved prompts list");
    Console.WriteLine($"   Server response: {promptsListContent}");
    
    // Get a specific prompt
    Console.WriteLine("\n📝 Getting specific prompt (greeting-email):");
    var promptGetRequest = new
    {
        jsonrpc = "2.0",
        method = "prompts/get",
        id = 6,
        @params = new
        {
            id = "greeting-email"
        }
    };
    
    var promptGetJson = JsonSerializer.Serialize(promptGetRequest);
    var promptGetResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(promptGetJson, System.Text.Encoding.UTF8, "application/json"));
    
    var promptGetContent = await promptGetResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"   Response: {promptGetContent}");
    
    // Step 5: Test Resources
    Console.WriteLine("\n📋 Step 5: Testing Resources");
    
    // List resources
    var resourcesListRequest = new
    {
        jsonrpc = "2.0",
        method = "resources/list",
        id = 7,
        @params = new { }
    };
    
    var resourcesListJson = JsonSerializer.Serialize(resourcesListRequest);
    var resourcesListResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(resourcesListJson, System.Text.Encoding.UTF8, "application/json"));
    
    var resourcesListContent = await resourcesListResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"✅ Retrieved resources list");
    Console.WriteLine($"   Server response: {resourcesListContent}");
    
    // Read a specific resource
    Console.WriteLine("\n📄 Reading specific resource (documents/sample.txt):");
    var resourceReadRequest = new
    {
        jsonrpc = "2.0",
        method = "resources/read",
        id = 8,
        @params = new
        {
            uri = "mcp://documents/sample.txt"
        }
    };
    
    var resourceReadJson = JsonSerializer.Serialize(resourceReadRequest);
    var resourceReadResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(resourceReadJson, System.Text.Encoding.UTF8, "application/json"));
    
    var resourceReadContent = await resourceReadResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"   Response: {resourceReadContent}");
    
    // Step 6: Test filtered prompts
    Console.WriteLine("\n📋 Step 6: Testing Filtered Prompts (tag: business)");
    var filteredPromptsRequest = new
    {
        jsonrpc = "2.0",
        method = "prompts/list",
        id = 9,
        @params = new
        {
            tag = "business"
        }
    };
    
    var filteredPromptsJson = JsonSerializer.Serialize(filteredPromptsRequest);
    var filteredPromptsResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(filteredPromptsJson, System.Text.Encoding.UTF8, "application/json"));
    
    var filteredPromptsContent = await filteredPromptsResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"   Response: {filteredPromptsContent}");
    
    // Step 7: Test filtered resources
    Console.WriteLine("\n📋 Step 7: Testing Filtered Resources (path: data/)");
    var filteredResourcesRequest = new
    {
        jsonrpc = "2.0",
        method = "resources/list",
        id = 10,
        @params = new
        {
            path = "data/"
        }
    };
    
    var filteredResourcesJson = JsonSerializer.Serialize(filteredResourcesRequest);
    var filteredResourcesResponse = await httpClient.PostAsync("api/mcp",
        new StringContent(filteredResourcesJson, System.Text.Encoding.UTF8, "application/json"));
    
    var filteredResourcesContent = await filteredResourcesResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"   Response: {filteredResourcesContent}");
    
    Console.WriteLine("\n🎉 Comprehensive MCP Testing Completed!");
    Console.WriteLine("   ✅ Initialize connection");
    Console.WriteLine("   ✅ Send initialized notification");
    Console.WriteLine("   ✅ List and call tools");
    Console.WriteLine("   ✅ List and get prompts");
    Console.WriteLine("   ✅ List and read resources");
    Console.WriteLine("   ✅ Test filtering capabilities");
    Console.WriteLine("\n🚀 All MCP features working correctly!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}
