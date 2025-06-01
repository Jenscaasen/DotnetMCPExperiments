namespace McpStreamFunc.Models;

/// <summary>
/// Request parameters for prompts/list method
/// </summary>
public class PromptsListParams
{
    public string? tag { get; set; }
    public int? limit { get; set; }
    public string? cursor { get; set; }
}

/// <summary>
/// Request parameters for prompts/get method
/// </summary>
public class PromptsGetParams
{
    public string id { get; set; } = string.Empty;
}

/// <summary>
/// Prompt item in the list response
/// </summary>
public class PromptItem
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string[]? tags { get; set; }
    public int size { get; set; }
    public string? sha256 { get; set; }
    public string? updated_at { get; set; }
}

/// <summary>
/// Full prompt details for get response
/// </summary>
public class Prompt
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string[]? tags { get; set; }
    public string text { get; set; } = string.Empty;
    public string? sha256 { get; set; }
    public string? updated_at { get; set; }
}

/// <summary>
/// Response for prompts/list method
/// </summary>
public class PromptsListResult
{
    public PromptItem[] items { get; set; } = Array.Empty<PromptItem>();
    public string? cursor { get; set; }
}