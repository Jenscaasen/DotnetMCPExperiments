namespace AspNetApiSse.Helpers;

/// <summary>
/// Helper class for handling resource template operations
/// </summary>
public static class ResourceTemplateHelper
{
    public static string? TryGetTemplateContent(string uri)
    {
        // Try to match URI against templates and generate content
        if (uri.StartsWith("mcp://templates/email/"))
        {
            var emailType = uri.Split('/').LastOrDefault();
            return $"Subject: {emailType} Email\n\nDear {{recipient}},\n\nThis is a {emailType} email template.\n\nBest regards,\n{{sender}}";
        }
        
        if (uri.StartsWith("mcp://templates/report/"))
        {
            var parts = uri.Split('/');
            if (parts.Length >= 6)
            {
                var month = parts[4];
                var year = parts[5];
                return $"# Monthly Report - {month}/{year}\n\n## Summary\nThis is the monthly report for {month}/{year}.\n\n## Key Metrics\n- Metric 1: Value\n- Metric 2: Value\n\n## Conclusion\nReport generated for {month}/{year}.";
            }
        }
        
        if (uri.StartsWith("mcp://templates/api/"))
        {
            var parts = uri.Split('/');
            if (parts.Length >= 6)
            {
                var service = parts[4];
                return $"openapi: 3.0.0\ninfo:\n  title: {service} API\n  version: 1.0.0\n  description: API specification for {service} service\npaths:\n  /{service}:\n    get:\n      summary: Get {service} data\n      responses:\n        '200':\n          description: Success";
            }
        }
        
        return null;
    }

    public static string GetMimeTypeForTemplate(string uri)
    {
        if (uri.StartsWith("mcp://templates/email/")) return "text/plain";
        if (uri.StartsWith("mcp://templates/report/")) return "text/markdown";
        if (uri.StartsWith("mcp://templates/api/")) return "application/yaml";
        return "text/plain";
    }
}