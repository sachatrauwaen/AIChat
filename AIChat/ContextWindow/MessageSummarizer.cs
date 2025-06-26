using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Satrabel.AIChat.ContextWindow
{
    // Message summarization service
    public class MessageSummarizer
    {
        public string SummarizeMessages(List<ContextAwareChatMessage> messages, int targetTokens)
        {
            var conversationText = string.Join("\n", messages.Select(m => $"{m.Role}: {m.Content}"));

            // Simple extractive summarization
            var sentences = SplitIntoSentences(conversationText);
            var importantSentences = ExtractImportantSentences(sentences, targetTokens);

            return string.Join(" ", importantSentences);
        }

        private List<string> SplitIntoSentences(string text)
        {
            return Regex.Split(text, @"(?<=[.!?])\s+")
                       .Where(s => !string.IsNullOrWhiteSpace(s))
                       .ToList();
        }

        private List<string> ExtractImportantSentences(List<string> sentences, int targetTokens)
        {
            // Simple heuristic: prefer sentences with questions, key terms, etc.
            var scoredSentences = sentences.Select(s => new
            {
                Sentence = s,
                Score = CalculateSentenceImportance(s)
            }).OrderByDescending(x => x.Score).ToList();

            var result = new List<string>();
            var tokenCount = 0;
            var estimator = new TokenEstimator();

            foreach (var item in scoredSentences)
            {
                var tokens = estimator.EstimateTokens(item.Sentence, TokenEstimationMethod.Simple);
                if (tokenCount + tokens <= targetTokens)
                {
                    result.Add(item.Sentence);
                    tokenCount += tokens;
                }
            }

            return result;
        }

        private double CalculateSentenceImportance(string sentence)
        {
            double score = 0;

            // Question sentences are important
            if (sentence.Contains('?')) score += 2;

            // Sentences with key terms
            var keyTerms = new[] { "important", "key", "main", "primary", "essential", "critical" };
            score += keyTerms.Count(term => sentence.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);

            // Length penalty for very long sentences
            if (sentence.Length > 200) score -= 1;

            // Bonus for sentences with numbers or data
            if (Regex.IsMatch(sentence, @"\d+")) score += 0.5;

            return score;
        }
    }
}