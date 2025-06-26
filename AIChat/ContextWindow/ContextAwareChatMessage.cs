using Satrabel.AIChat.History;

namespace Satrabel.AIChat.ContextWindow
{
    // Enhanced ChatMessage with context management features
    public class ContextAwareChatMessage : ChatMessage
    {
        public int EstimatedTokens { get; set; }
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;
        public bool IsSystemMessage => Role == MessageRole.System;
        public bool IsPinned { get; set; } = false;
        public string Summary { get; set; }
        public bool CanBeSummarized { get; set; } = true;

        public ContextAwareChatMessage() { }

        public ContextAwareChatMessage(MessageRole role, string content, MessagePriority priority = MessagePriority.Normal)
            : base(role, content)
        {
            Priority = priority;
        }
    }
}