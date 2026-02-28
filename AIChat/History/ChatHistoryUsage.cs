namespace Satrabel.AIChat.History
{
    /// <summary>
    /// Summary of how much history was used when building
    /// the model context for a single request.
    /// </summary>
    public class ChatHistoryUsage
    {
        public int TotalMessages { get; set; }
        public int KeptMessages { get; set; }

        public int TotalApproxTokens { get; set; }
        public int KeptApproxTokens { get; set; }

        public int TotalUserMessages { get; set; }
        public int TotalAssistantMessages { get; set; }
        public int TotalSystemMessages { get; set; }
        public int TotalToolMessages { get; set; }

        public int KeptUserMessages { get; set; }
        public int KeptAssistantMessages { get; set; }
        public int KeptSystemMessages { get; set; }
        public int KeptToolMessages { get; set; }

        public int DroppedMessages => TotalMessages - KeptMessages;

        public int DroppedApproxTokens => TotalApproxTokens - KeptApproxTokens;
    }
}

