namespace Satrabel.AIChat.ContextWindow
{
    // Context window trimming strategies
    public enum TrimmingStrategy
    {
        TruncateOldest,     // Remove oldest messages first
        TruncateMiddle,     // Keep system + recent messages, remove middle
        Summarize,          // Summarize old messages
        SlidingWindow,      // Keep last N messages
        Importance,         // Keep most important messages
        Hybrid             // Combination of strategies
    }
}