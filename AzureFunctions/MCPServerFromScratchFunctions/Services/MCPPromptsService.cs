using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpStreamFunc.Models;

namespace McpStreamFunc.Services;

/// <summary>
/// Service for handling MCP prompt operations
/// </summary>
public class MCPPromptsService
{
    private static readonly List<Prompt> _prompts = new()
    {
        new Prompt
        {
            id = "greeting-email",
            name = "Professional Email Greeting",
            tags = new[] { "email", "business", "greeting" },
            text = "Hello {{name}},\n\nThank you for reaching out to us. We appreciate your interest in our services.\n\nBest regards,\n{{sender_name}}",
            sha256 = ComputeSha256("Hello {{name}},\n\nThank you for reaching out to us. We appreciate your interest in our services.\n\nBest regards,\n{{sender_name}}"),
            updated_at = "2024-11-05T10:30:00Z"
        },
        new Prompt
        {
            id = "code-review-template",
            name = "Code Review Template",
            tags = new[] { "development", "code-review", "template" },
            text = "## Code Review for {{pull_request_title}}\n\n### Summary\n{{summary}}\n\n### Changes Reviewed\n- {{changes}}\n\n### Feedback\n{{feedback}}\n\n### Approval Status\n{{status}}",
            sha256 = ComputeSha256("## Code Review for {{pull_request_title}}\n\n### Summary\n{{summary}}\n\n### Changes Reviewed\n- {{changes}}\n\n### Feedback\n{{feedback}}\n\n### Approval Status\n{{status}}"),
            updated_at = "2024-11-05T14:22:00Z"
        },
        new Prompt
        {
            id = "meeting-agenda",
            name = "Meeting Agenda Template",
            tags = new[] { "meeting", "agenda", "business" },
            text = "# Meeting Agenda\n\n**Date:** {{date}}\n**Time:** {{time}}\n**Attendees:** {{attendees}}\n\n## Agenda Items\n1. {{item1}}\n2. {{item2}}\n3. {{item3}}\n\n## Action Items\n- {{action1}}\n- {{action2}}",
            sha256 = ComputeSha256("# Meeting Agenda\n\n**Date:** {{date}}\n**Time:** {{time}}\n**Attendees:** {{attendees}}\n\n## Agenda Items\n1. {{item1}}\n2. {{item2}}\n3. {{item3}}\n\n## Action Items\n- {{action1}}\n- {{action2}}"),
            updated_at = "2024-11-05T09:15:00Z"
        }
    };

    public object ListPrompts(object? parameters)
    {
        try
        {
            PromptsListParams? listParams = null;
            
            if (parameters != null)
            {
                var json = JsonSerializer.Serialize(parameters);
                listParams = JsonSerializer.Deserialize<PromptsListParams>(json);
            }

            var filteredPrompts = _prompts.AsEnumerable();

            // Apply tag filter if specified
            if (!string.IsNullOrEmpty(listParams?.tag))
            {
                filteredPrompts = filteredPrompts.Where(p => p.tags?.Contains(listParams.tag) == true);
            }

            // Apply pagination (simplified - in real implementation you'd use proper cursor-based pagination)
            var limit = listParams?.limit ?? 50;
            var promptsList = filteredPrompts.Take(limit).ToList();

            var items = promptsList.Select(p => new PromptItem
            {
                id = p.id,
                name = p.name,
                tags = p.tags,
                size = p.text.Length,
                sha256 = p.sha256,
                updated_at = p.updated_at
            }).ToArray();

            // Return MCP-compliant format with direct prompts array
            // Only include nextCursor if there are more items (don't include null)
            if (promptsList.Count >= limit)
            {
                return new
                {
                    prompts = items,
                    nextCursor = "next_page_cursor"
                };
            }

            return new
            {
                prompts = items
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error listing prompts: {ex.Message}", ex);
        }
    }

    public object GetPrompt(object? parameters)
    {
        try
        {
            if (parameters == null)
            {
                throw new ArgumentException("Prompt ID or name is required");
            }

            var json = JsonSerializer.Serialize(parameters);
            
            // Try to parse as a generic object to handle both id and name
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            string? promptId = null;
            string? promptName = null;
            
            if (root.TryGetProperty("id", out var idElement))
            {
                promptId = idElement.GetString();
            }
            
            if (root.TryGetProperty("name", out var nameElement))
            {
                promptName = nameElement.GetString();
            }

            if (string.IsNullOrEmpty(promptId) && string.IsNullOrEmpty(promptName))
            {
                throw new ArgumentException("Prompt ID or name is required");
            }

            // Try to find by ID first, then by name
            Prompt? prompt = null;
            
            if (!string.IsNullOrEmpty(promptId))
            {
                prompt = _prompts.FirstOrDefault(p => p.id == promptId);
            }
            
            if (prompt == null && !string.IsNullOrEmpty(promptName))
            {
                prompt = _prompts.FirstOrDefault(p => p.name == promptName);
            }
            
            if (prompt == null)
            {
                var searchTerm = !string.IsNullOrEmpty(promptId) ? promptId : promptName;
                throw new InvalidOperationException($"Prompt not found: {searchTerm}");
            }

            // Return MCP-compliant format with prompt metadata and messages array
            return new
            {
                prompt = new
                {
                    id = prompt.id,
                    name = prompt.name,
                    description = $"Template for {prompt.name.ToLower()}",
                    tags = prompt.tags
                },
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new
                        {
                            type = "text",
                            text = prompt.text
                        }
                    }
                },
                variables = GetPromptVariables(prompt.text)
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error getting prompt: {ex.Message}", ex);
        }
    }

    private static object GetPromptVariables(string templateText)
    {
        // Extract variables from template text ({{variable}} format)
        var variables = new Dictionary<string, object>();
        var matches = System.Text.RegularExpressions.Regex.Matches(templateText, @"\{\{(\w+)\}\}");
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var variableName = match.Groups[1].Value;
            if (!variables.ContainsKey(variableName))
            {
                variables[variableName] = new
                {
                    type = "string",
                    label = FormatVariableLabel(variableName)
                };
            }
        }
        
        return variables;
    }
    
    private static string FormatVariableLabel(string variableName)
    {
        // Convert camelCase/snake_case to human readable format
        var result = System.Text.RegularExpressions.Regex.Replace(variableName, @"([a-z])([A-Z])", "$1 $2");
        result = result.Replace("_", " ");
        return char.ToUpper(result[0]) + result.Substring(1).ToLower();
    }

    private static string ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}