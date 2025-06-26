using System;

namespace Satrabel.AIChat.History
{
    // Statistics class
    public class ConversationStats
    {
        public int TotalMessages { get; set; }
        public int UserMessages { get; set; }
        public int AssistantMessages { get; set; }
        public int SystemMessages { get; set; }
        public int EstimatedTokens { get; set; }
        public DateTime? FirstMessageTime { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public TimeSpan ConversationDuration { get; set; }
    }
}