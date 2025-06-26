using System;

namespace Satrabel.AIChat.History
{
    // Class representing a single chat message
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public ChatMessage() { }

        public ChatMessage(MessageRole role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}