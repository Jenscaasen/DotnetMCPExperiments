using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpStreamFunc.Models;

namespace McpStreamFunc.Services;

/// <summary>
/// Service for handling MCP resource operations
/// </summary>
public class MCPResourcesService
{
    private static readonly List<ResourceItem> _resources = new()
    {
        new ResourceItem
        {
            uri = "mcp://documents/sample.txt",
            name = "Sample Text Document",
            mimeType = "text/plain",
            description = "A sample text file demonstrating MCP resource functionality",
            size = 45,
            checksum = "sha256:" + ComputeSha256("This is a sample text file for MCP resources."),
            created = "2024-11-05T10:00:00Z",
            modified = "2024-11-05T10:00:00Z",
            version = 1,
            tags = new[] { "sample", "text", "demo" }
        },
        new ResourceItem
        {
            uri = "mcp://data/config.json",
            name = "Server Configuration",
            mimeType = "application/json",
            description = "Configuration file for the MCP server",
            size = 156,
            checksum = "sha256:" + ComputeSha256("{\"name\":\"MCP Server\",\"version\":\"1.0.0\",\"features\":[\"tools\",\"prompts\",\"resources\"]}"),
            created = "2024-11-05T12:30:00Z",
            modified = "2024-11-05T12:30:00Z",
            version = 1,
            tags = new[] { "config", "json", "server" }
        },
        new ResourceItem
        {
            uri = "mcp://images/logo.png",
            name = "Company Logo",
            mimeType = "image/png",
            description = "Corporate logo image in PNG format",
            size = 2048,
            checksum = "sha256:fake_png_hash_for_demo",
            created = "2024-11-05T08:45:00Z",
            modified = "2024-11-05T08:45:00Z",
            version = 1,
            tags = new[] { "image", "logo", "branding" }
        },
        new ResourceItem
        {
            uri = "mcp://scripts/hello.py",
            name = "Hello World Script",
            mimeType = "text/x-python",
            description = "A simple Python script that prints greetings",
            size = 87,
            checksum = "sha256:" + ComputeSha256("#!/usr/bin/env python3\nprint(\"Hello from MCP Resources!\")\nprint(\"This is a demo script.\")"),
            created = "2024-11-05T15:20:00Z",
            modified = "2024-11-05T15:20:00Z",
            version = 1,
            tags = new[] { "script", "python", "demo" }
        }
    };

    private static readonly List<ResourceTemplateItem> _resourceTemplates = new()
    {
        new ResourceTemplateItem
        {
            uriTemplate = "mcp://templates/email/{type}",
            name = "Professional Email Template",
            description = "Template for creating professional business emails",
            mimeType = "text/plain"
        },
        new ResourceTemplateItem
        {
            uriTemplate = "mcp://templates/report/{month}/{year}",
            name = "Monthly Report Template",
            description = "Template for generating monthly business reports",
            mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        },
        new ResourceTemplateItem
        {
            uriTemplate = "mcp://templates/api/{service}/spec",
            name = "API Specification Template",
            description = "OpenAPI 3.0 template for REST API documentation",
            mimeType = "application/yaml"
        }
    };

    private static readonly Dictionary<string, string> _resourceContents = new()
    {
        ["mcp://documents/sample.txt"] = "This is a sample text file for MCP resources.",
        ["mcp://data/config.json"] = "{\"name\":\"MCP Server\",\"version\":\"1.0.0\",\"features\":[\"tools\",\"prompts\",\"resources\"]}",
        ["mcp://images/logo.png"] = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==", // 1x1 transparent PNG in base64
        ["mcp://scripts/hello.py"] = "#!/usr/bin/env python3\nprint(\"Hello from MCP Resources!\")\nprint(\"This is a demo script.\")"
    };

    private string? TryGetTemplateContent(string uri)
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

    private string GetMimeTypeForTemplate(string uri)
    {
        if (uri.StartsWith("mcp://templates/email/")) return "text/plain";
        if (uri.StartsWith("mcp://templates/report/")) return "text/markdown";
        if (uri.StartsWith("mcp://templates/api/")) return "application/yaml";
        return "text/plain";
    }

    public object ListResources(object? parameters)
    {
        try
        {
            ResourcesListParams? listParams = null;
            
            if (parameters != null)
            {
                var json = JsonSerializer.Serialize(parameters);
                listParams = JsonSerializer.Deserialize<ResourcesListParams>(json);
            }

            var filteredResources = _resources.AsEnumerable();

            // Apply path filter if specified (filter by URI path)
            if (!string.IsNullOrEmpty(listParams?.path))
            {
                filteredResources = filteredResources.Where(r => r.uri.Contains(listParams.path));
            }

            // Apply pagination (simplified - in real implementation you'd use proper cursor-based pagination)
            var limit = listParams?.limit ?? 100;
            var resourcesList = filteredResources.Take(limit).ToList();

            // Return MCP-compliant format according to specification
            var result = new
            {
                resources = resourcesList.ToArray()
            };

            // Only include nextCursor if there are more items (don't include null)
            if (resourcesList.Count >= limit)
            {
                return new
                {
                    resources = resourcesList.ToArray(),
                    nextCursor = "next_page_cursor"
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error listing resources: {ex.Message}", ex);
        }
    }

    public object ListResourceTemplates(object? parameters)
    {
        try
        {
            ResourcesListParams? listParams = null;
            
            if (parameters != null)
            {
                var json = JsonSerializer.Serialize(parameters);
                listParams = JsonSerializer.Deserialize<ResourcesListParams>(json);
            }

            var filteredTemplates = _resourceTemplates.AsEnumerable();

            // Apply path filter if specified (filter by URI template path)
            if (!string.IsNullOrEmpty(listParams?.path))
            {
                filteredTemplates = filteredTemplates.Where(r => r.uriTemplate.Contains(listParams.path));
            }

            // Apply pagination
            var limit = listParams?.limit ?? 100;
            var templatesList = filteredTemplates.Take(limit).ToList();

            // Return MCP-compliant format for resource templates
            var result = new
            {
                resourceTemplates = templatesList.ToArray()
            };

            // Only include nextCursor if there are more items
            if (templatesList.Count >= limit)
            {
                return new
                {
                    resourceTemplates = templatesList.ToArray(),
                    nextCursor = "next_page_cursor"
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error listing resource templates: {ex.Message}", ex);
        }
    }

    public object ReadResource(object? parameters)
    {
        try
        {
            if (parameters == null)
            {
                throw new ArgumentException("Resource URI is required");
            }

            var json = JsonSerializer.Serialize(parameters);
            var readParams = JsonSerializer.Deserialize<ResourcesReadParams>(json);

            if (string.IsNullOrEmpty(readParams?.uri))
            {
                throw new ArgumentException("Resource URI is required");
            }

            // First try to find static resource by URI
            var resource = _resources.FirstOrDefault(r => r.uri == readParams.uri);
            string content;
            string mimeType;

            if (resource != null)
            {
                // Static resource found
                if (!_resourceContents.TryGetValue(readParams.uri, out content!))
                {
                    throw new InvalidOperationException($"Resource content not available: {readParams.uri}");
                }
                mimeType = resource.mimeType;
            }
            else
            {
                // Try to resolve as template resource
                content = TryGetTemplateContent(readParams.uri)!;
                if (content == null)
                {
                    throw new InvalidOperationException($"Resource not found: {readParams.uri}");
                }
                mimeType = GetMimeTypeForTemplate(readParams.uri);
            }

            // Create content object based on MIME type
            var resourceContent = new ResourceContent
            {
                uri = readParams.uri,
                mimeType = mimeType
            };

            // For text content, use text field; for binary content, use blob field
            if (mimeType.StartsWith("text/") || mimeType == "application/json" || mimeType == "application/yaml" || mimeType == "text/markdown")
            {
                resourceContent.text = content;
            }
            else
            {
                // For binary content (images, etc.), use blob field with base64 encoding
                resourceContent.blob = content;
            }

            return new ResourceReadResult
            {
                contents = new[] { resourceContent }
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading resource: {ex.Message}", ex);
        }
    }

    private static string ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}