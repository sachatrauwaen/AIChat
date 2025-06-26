using System;
using System.Text.RegularExpressions;

namespace Satrabel.AIChat.ContextWindow
{
    // Token estimation service
    public class TokenEstimator
    {
        public int EstimateTokens(string text, TokenEstimationMethod method)
        {
            switch (method)
            {
                case TokenEstimationMethod.Simple:
                    return EstimateSimple(text);
                case TokenEstimationMethod.GPT:
                    return EstimateGPT(text);
                case TokenEstimationMethod.Claude:
                    return EstimateClaude(text);
                case TokenEstimationMethod.Exact:
                    return EstimateExact(text);
                default:
                    return EstimateSimple(text);
            }
        }

        private int EstimateSimple(string text)
        {
            return Math.Max(1, text.Length / 4);
        }

        private int EstimateGPT(string text)
        {
            // More sophisticated estimation for GPT models
            // Account for common patterns, punctuation, etc.
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var baseTokens = words.Length;

            // Adjustments
            var punctuationCount = Regex.Matches(text, @"[.!?;:,]").Count;
            var specialChars = Regex.Matches(text, @"[^\w\s]").Count;

            return Math.Max(1, (int)(baseTokens * 1.3 + punctuationCount * 0.5 + specialChars * 0.3));
        }

        private int EstimateClaude(string text)
        {
            // Claude typically has different tokenization patterns
            var baseEstimate = EstimateGPT(text);
            return Math.Max(1, (int)(baseEstimate * 0.9)); // Claude often uses fewer tokens
        }

        private int EstimateExact(string text)
        {
            // Placeholder for exact tokenization using external library
            // In practice, you'd use libraries like TikToken, SharpToken, etc.
            return EstimateGPT(text);
        }
    }
}