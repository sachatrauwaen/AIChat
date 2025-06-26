using System;
using System.Collections.Generic;
using System.Linq;

namespace Satrabel.AIChat.History
{
    // Class representing a conversation/chat session
    public class ChatConversation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "New Conversation";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        public void AddMessage(ChatMessage message)
        {
            Messages.Add(message);
            LastModified = DateTime.UtcNow;
        }

        public void AddMessage(MessageRole role, string content)
        {
            AddMessage(new ChatMessage(role, content));
        }

        public ChatMessage GetLastMessage() => Messages.LastOrDefault();

        public List<ChatMessage> GetMessagesByRole(MessageRole role)
        {
            return Messages.Where(m => m.Role == role).ToList();
        }

        public int GetTokenCount()
        {
            // Simple approximation: 4 chars = 1 token
            return Messages.Sum(m => m.Content.Length) / 4;
        }
    }
}