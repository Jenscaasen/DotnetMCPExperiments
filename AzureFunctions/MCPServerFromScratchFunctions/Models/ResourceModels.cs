namespace McpStreamFunc.Models;

/// <summary>
/// Request parameters for resources/list method
/// </summary>
public class ResourcesListParams
{
    public string? path { get; set; }
    public int? limit { get; set; }
    public string? cursor { get; set; }
}

/// <summary>
/// Request parameters for resources/read method
/// </summary>
public class ResourcesReadParams
{
    public string uri { get; set; } = string.Empty;
    public int? offset { get; set; }
    public int? length { get; set; }
}

/// <summary>
/// Resource item in the list response - MCP compliant
/// </summary>
public class ResourceItem
{
    public string uri { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string mimeType { get; set; } = string.Empty;
    public string? description { get; set; }
    public long? size { get; set; }
    public string? checksum { get; set; }
    public string? created { get; set; }
    public string? modified { get; set; }
    public int? version { get; set; }
    public string[]? tags { get; set; }
}

/// <summary>
/// Resource template item for templates/list response - MCP compliant
/// </summary>
public class ResourceTemplateItem
{
    public string uriTemplate { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string? mimeType { get; set; }
}

/// <summary>
/// Response for resources/list method
/// </summary>
public class ResourcesListResult
{
    public ResourceItem[] items { get; set; } = Array.Empty<ResourceItem>();
    public string? cursor { get; set; }
}

/// <summary>
/// Content item for resources/read response
/// </summary>
public class ResourceContent
{
    public string uri { get; set; } = string.Empty;
    public string mimeType { get; set; } = string.Empty;
    public string? text { get; set; }
    public string? blob { get; set; } // base64-encoded binary data
}

/// <summary>
/// Response for resources/read method - MCP compliant
/// </summary>
public class ResourceReadResult
{
    public ResourceContent[] contents { get; set; } = Array.Empty<ResourceContent>();
}