namespace Satrabel.AIChat.ContextWindow
{
    // Context window configuration
    public class ContextWindowConfig
    {
        public int MaxTokens { get; set; } = 4000;
        public int TargetTokens { get; set; } = 3500; // Leave buffer for response
        public int MinTokensToKeep { get; set; } = 1000;
        public TokenEstimationMethod EstimationMethod { get; set; } = TokenEstimationMethod.GPT;
        public TrimmingStrategy Strategy { get; set; } = TrimmingStrategy.Hybrid;
        public bool PreserveSystemMessages { get; set; } = true;
        public bool PreservePinnedMessages { get; set; } = true;
        public int MaxMessagesToKeep { get; set; } = 50;
        public double SummarizationThreshold { get; set; } = 0.6; // Summarize when reaching 60% of context
    }
}