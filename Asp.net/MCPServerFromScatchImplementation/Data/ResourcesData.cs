using System.Security.Cryptography;
using System.Text;
using AspNetApiSse.Models;

namespace AspNetApiSse.Data;

/// <summary>
/// Static data for MCP resources demonstration
/// </summary>
public static class ResourcesData
{
    public static readonly List<ResourceItem> Resources = new()
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

    public static readonly List<ResourceTemplateItem> ResourceTemplates = new()
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

    public static readonly Dictionary<string, string> ResourceContents = new()
    {
        ["mcp://documents/sample.txt"] = "This is a sample text file for MCP resources.",
        ["mcp://data/config.json"] = "{\"name\":\"MCP Server\",\"version\":\"1.0.0\",\"features\":[\"tools\",\"prompts\",\"resources\"]}",
        ["mcp://images/logo.png"] = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==", // 1x1 transparent PNG in base64
        ["mcp://scripts/hello.py"] = "#!/usr/bin/env python3\nprint(\"Hello from MCP Resources!\")\nprint(\"This is a demo script.\")"
    };

    private static string ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}