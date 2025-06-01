using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using McpStreamFunc.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Configure logging
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Add MCP Services with dependency injection
        services.AddScoped<MCPToolsService>();
        services.AddScoped<MCPPromptsService>();
        services.AddScoped<MCPResourcesService>();
        services.AddScoped<MCPMessageProcessor>();
        services.AddScoped<LegacyEventProcessor>();
    })
    .Build();

await host.RunAsync();
