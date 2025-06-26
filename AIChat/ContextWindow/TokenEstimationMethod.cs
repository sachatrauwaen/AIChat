namespace Satrabel.AIChat.ContextWindow
{
    // Token estimation strategies
    public enum TokenEstimationMethod
    {
        Simple,      // 4 chars = 1 token (rough estimate)
        GPT,         // More accurate for GPT models
        Claude,      // Optimized for Claude models
        Exact        // Using actual tokenizer (would need external library)
    }
}