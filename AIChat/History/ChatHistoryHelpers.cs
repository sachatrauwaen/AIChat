using System;

namespace Satrabel.AIChat.History
{
    /// <summary>
    /// Shared helpers for reasoning about chat history messages.
    /// </summary>
    public static class ChatHistoryHelpers
    {
        private const string ToolResultPrefix = "[Tool Result for ";

        /// <summary>
        /// Returns true when the message is a synthetic wrapper around a tool
        /// result, e.g. "[Tool Result for SomeTool]: ...".
        /// These messages are useful for UI but should generally be excluded
        /// from model context.
        /// </summary>
        public static bool IsToolResultWrapper(ChatMessage message)
        {
            if (message == null)
            {
                return false;
            }

            if (message.Role != MessageRole.User)
            {
                return false;
            }

            var content = message.Content;
            return !string.IsNullOrEmpty(content) &&
                   content.StartsWith(ToolResultPrefix, StringComparison.Ordinal);
        }
    }
}

