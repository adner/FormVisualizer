using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.AI;

namespace FormVisualizer.Services;

/// <summary>
/// Helper class for converting between different chat message formats
/// </summary>
public static class ChatHistoryHelper
{
    /// <summary>
    /// Converts a collection of ChatMessage objects to a Semantic Kernel ChatHistory
    /// </summary>
    /// <param name="messages">The collection of chat messages to convert</param>
    /// <param name="initialSystem">Optional initial system message to add to the history</param>
    /// <returns>A ChatHistory object containing the converted messages</returns>
    public static ChatHistory ToChatHistory(IEnumerable<ChatMessage> messages, string? initialSystem = null)
    {
        var history = string.IsNullOrWhiteSpace(initialSystem)
            ? new ChatHistory()
            : new ChatHistory(initialSystem); // adds a system message

        foreach (var m in messages)
        {
            // Prefer ChatMessage.Text; if null, glue together any TextContent parts
            var text = m.Text;
            if (text is null && m.Contents is { Count: >0 })
            {
                text = string.Join("",
                    m.Contents.OfType<Microsoft.Extensions.AI.TextContent>().Select(tc => tc.Text));
            }

            text ??= string.Empty;

            switch (m.AuthorName?.ToLowerInvariant())
            {
                case "system":
                    history.AddSystemMessage(text);
                    break;
                case "developer":
                    history.AddDeveloperMessage(text);
                    break;
                case "user":
                    history.AddUserMessage(text);
                    break;
                case "assistant":
                    history.AddAssistantMessage(text);
                    break;
                case "tool":
                    // If you track tool name/id, pass via metadata (last param)
                    history.AddMessage(AuthorRole.Tool, text);
                    break;
                default:
                    // Treat unknown roles as user
                    history.AddUserMessage(text);
                    break;
            }
        }

        return history;
    }
}