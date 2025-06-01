using AspNetApiSse.Services;

namespace AspNetApiSse.Extensions;

/// <summary>
/// Extension methods for configuring application services
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Configures MCP services for dependency injection
    /// </summary>
    public static IServiceCollection AddMCPServices(this IServiceCollection services)
    {
        services.AddScoped<MCPToolsService>();
        services.AddScoped<MCPPromptsService>();
        services.AddScoped<MCPResourcesService>();
        services.AddScoped<MCPMessageProcessor>();
        services.AddScoped<LegacyEventProcessor>();
        services.AddSingleton<SSEConnectionManager>();
        
        return services;
    }

    /// <summary>
    /// Configures CORS policy for the application
    /// </summary>
    public static IServiceCollection AddMCPCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
        
        return services;
    }

    /// <summary>
    /// Configures logging for the application
    /// </summary>
    public static IServiceCollection AddMCPLogging(this IServiceCollection services, ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
        
        return services;
    }
}