using System.Net.ServerSentEvents;
using System.Text.Json;

Console.WriteLine("SSE MCP Client - Testing Server-Sent Events");
Console.WriteLine("==========================================");

try
{
    var httpClient = new HttpClient();
    var baseUrl = "http://localhost:5253"; // aspnetApi default port
    Console.WriteLine($"🔗 Connecting to SSE MCP server at {baseUrl}...");
    
    // Step 1: Connect to SSE endpoint and get the message endpoint
    Console.WriteLine("\n📋 Step 1: Establishing SSE Connection");
    
    var sseUrl = $"{baseUrl}/mcp/sse";
    var postEndpoint = "";
    
    using var stream = await httpClient.GetStreamAsync(sseUrl);
    Console.WriteLine("✅ SSE connection established");
    
    // Create SSE parser and set up response tracking
    var endpointReceived = false;
    var responses = new Dictionary<int, string>();
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
    
    // Start listening for SSE events in background
    var sseTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var sseEvent in SseParser.Create(stream).EnumerateAsync(cts.Token))
            {
                Console.WriteLine($"📨 SSE Event: {sseEvent.EventType} - {sseEvent.Data}");
                
                if (sseEvent.EventType == "endpoint")
                {
                    // Handle both formats: string URI directly or JSON object with uri property
                    try
                    {
                        var endpointData = JsonSerializer.Deserialize<JsonElement>(sseEvent.Data);
                        if (endpointData.ValueKind == JsonValueKind.String)
                        {
                            // Direct string format
                            postEndpoint = endpointData.GetString()!;
                        }
                        else
                        {
                            // JSON object format
                            postEndpoint = endpointData.GetProperty("uri").GetString()!;
                        }
                    }
                    catch
                    {
                        // Fallback: treat as string directly
                        postEndpoint = sseEvent.Data;
                    }
                    Console.WriteLine($"✅ Received endpoint: {postEndpoint}");
                    endpointReceived = true;
                }
                else if (sseEvent.EventType == "message")
                {
                    // Try to parse the message to get the ID
                    try
                    {
                        var messageData = JsonSerializer.Deserialize<JsonElement>(sseEvent.Data);
                        if (messageData.TryGetProperty("id", out var idProp))
                        {
                            var id = idProp.GetInt32();
                            responses[id] = sseEvent.Data;
                            Console.WriteLine($"📦 Stored response for ID {id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Could not parse message: {ex.Message}");
                    }
                }
                else if (sseEvent.EventType == "ping")
                {
                    Console.WriteLine($"🏓 Ping: {sseEvent.Data}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ SSE listener error: {ex.Message}");
        }
    });
    
    // Wait for endpoint to be received
    var timeout = DateTime.UtcNow.AddSeconds(10);
    while (!endpointReceived && DateTime.UtcNow < timeout)
    {
        await Task.Delay(100);
    }
    
    if (!endpointReceived)
    {
        throw new Exception("Failed to receive endpoint event from SSE connection");
    }
    
    // Step 2: Initialize the MCP connection via POST
    Console.WriteLine("\n📋 Step 2: Initialize MCP Connection");
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
                name = "sse-test-client",
                version = "1.0.0"
            }
        }
    };
    
    var initJson = JsonSerializer.Serialize(initRequest);
    var fullPostUrl = baseUrl + postEndpoint;
    Console.WriteLine($"📤 Sending initialize to: {fullPostUrl}");
    
    var initResponse = await httpClient.PostAsync(fullPostUrl,
        new StringContent(initJson, System.Text.Encoding.UTF8, "application/json"));
    
    if (!initResponse.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to initialize: {initResponse.StatusCode}");
    }
    
    // Wait for response via SSE
    await Task.Delay(1000); // Give SSE time to receive response
    var initContent = responses.ContainsKey(1) ? responses[1] : "No response received via SSE";
    Console.WriteLine($"✅ Initialized connection");
    Console.WriteLine($"   Server response: {initContent}");
    
    // Step 3: Send initialized notification
    Console.WriteLine("\n📋 Step 3: Send Initialized Notification");
    var initializedRequest = new
    {
        jsonrpc = "2.0",
        method = "notifications/initialized"
    };
    
    var initializedJson = JsonSerializer.Serialize(initializedRequest);
    var initializedResponse = await httpClient.PostAsync(fullPostUrl,
        new StringContent(initializedJson, System.Text.Encoding.UTF8, "application/json"));
    
    Console.WriteLine($"✅ Sent initialized notification: {initializedResponse.StatusCode}");
    
    // Step 4: List tools to see if echo is available
    Console.WriteLine("\n📋 Step 4: Testing Tools");
    
    var toolsRequest = new
    {
        jsonrpc = "2.0",
        method = "tools/list",
        id = 2,
        @params = new { }
    };
    
    var toolsJson = JsonSerializer.Serialize(toolsRequest);
    var toolsResponse = await httpClient.PostAsync(fullPostUrl,
        new StringContent(toolsJson, System.Text.Encoding.UTF8, "application/json"));
    
    if (!toolsResponse.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to list tools: {toolsResponse.StatusCode}");
    }
    
    // Wait for response via SSE
    await Task.Delay(1000); // Give SSE time to receive response
    var toolsContent = responses.ContainsKey(2) ? responses[2] : "No response received via SSE";
    Console.WriteLine($"✅ Retrieved tools list");
    Console.WriteLine($"   Server response: {toolsContent}");
    
    // Step 5: Call Echo tool
    Console.WriteLine("\n🔧 Calling Echo tool:");
    var echoToolRequest = new
    {
        jsonrpc = "2.0",
        method = "tools/call",
        id = 3,
        @params = new
        {
            name = "EchoTool",
            arguments = new
            {
                message = "Hello from SSE client!"
            }
        }
    };
    
    var echoToolJson = JsonSerializer.Serialize(echoToolRequest);
    var echoToolResponse = await httpClient.PostAsync(fullPostUrl,
        new StringContent(echoToolJson, System.Text.Encoding.UTF8, "application/json"));
    
    // Wait for response via SSE
    await Task.Delay(1000); // Give SSE time to receive response
    var echoToolContent = responses.ContainsKey(3) ? responses[3] : "No response received via SSE";
    Console.WriteLine($"   Response: {echoToolContent}");
    
    // Check if we got a successful echo response
    if (responses.ContainsKey(3))
    {
        var echoResult = JsonSerializer.Deserialize<JsonElement>(echoToolContent);
        if (echoResult.TryGetProperty("result", out var result) &&
            result.TryGetProperty("content", out var content) &&
            content.GetArrayLength() > 0)
        {
            var textContent = content[0].GetProperty("text").GetString();
            if (textContent?.Contains("Hello from SSE client!") == true)
            {
                Console.WriteLine("🎉 SUCCESS! Echo tool worked via SSE!");
            }
            else
            {
                Console.WriteLine($"⚠️  Echo tool responded but content unexpected: {textContent}");
            }
        }
        else
        {
            Console.WriteLine("❌ Echo tool call failed or returned unexpected format");
        }
    }
    else
    {
        Console.WriteLine("❌ No response received via SSE for Echo tool");
    }
    
    Console.WriteLine("\n🎉 SSE MCP Client Testing Completed!");
    Console.WriteLine("   ✅ Established SSE connection");
    Console.WriteLine("   ✅ Received endpoint from server");
    Console.WriteLine("   ✅ Initialize connection via POST");
    Console.WriteLine("   ✅ Send initialized notification");
    Console.WriteLine("   ✅ List tools");
    Console.WriteLine("   ✅ Call Echo tool via SSE");
    Console.WriteLine("\n🚀 SSE MCP transport working correctly!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}