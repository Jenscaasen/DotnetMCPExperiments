using System.Text.Json;
using AspNetApiSse.Models;
using AspNetApiSse.Data;
using AspNetApiSse.Helpers;

namespace AspNetApiSse.Services;

/// <summary>
/// Service for handling MCP resource operations
/// </summary>
public class MCPResourcesService
{

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

            var filteredResources = ResourcesData.Resources.AsEnumerable();

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

            var filteredTemplates = ResourcesData.ResourceTemplates.AsEnumerable();

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
            var resource = ResourcesData.Resources.FirstOrDefault(r => r.uri == readParams.uri);
            string? content;
            string mimeType;

            if (resource != null)
            {
                // Static resource found
                if (!ResourcesData.ResourceContents.TryGetValue(readParams.uri, out content))
                {
                    throw new InvalidOperationException($"Resource content not available: {readParams.uri}");
                }
                mimeType = resource.mimeType;
            }
            else
            {
                // Try to resolve as template resource
                content = ResourceTemplateHelper.TryGetTemplateContent(readParams.uri);
                if (content == null)
                {
                    throw new InvalidOperationException($"Resource not found: {readParams.uri}");
                }
                mimeType = ResourceTemplateHelper.GetMimeTypeForTemplate(readParams.uri);
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
}