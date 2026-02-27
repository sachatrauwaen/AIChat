using System;
using System.Collections.Generic;

namespace Satrabel.AIChat.History
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ToolName { get; set; }
        public Dictionary<string, object> ToolArguments { get; set; }
        public string ToolCallId { get; set; }

        public ChatMessage() { }

        public ChatMessage(MessageRole role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}