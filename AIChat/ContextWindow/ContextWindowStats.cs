namespace Satrabel.AIChat.ContextWindow
{
    public class ContextWindowStats
    {
        public int TotalMessages { get; set; }
        public int TotalTokens { get; set; }
        public int MaxTokens { get; set; }
        public int TargetTokens { get; set; }
        public double UtilizationPercentage { get; set; }
        public int SystemMessages { get; set; }
        public int PinnedMessages { get; set; }
        public double AverageTokensPerMessage { get; set; }
        public TokenEstimationMethod EstimationMethod { get; set; }
        public TrimmingStrategy TrimmingStrategy { get; set; }
    }
}