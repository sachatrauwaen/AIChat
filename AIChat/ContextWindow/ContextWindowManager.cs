using Satrabel.AIChat.History;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Satrabel.AIChat.ContextWindow
{

    // Main context window manager
    public class ContextWindowManager
    {
        private readonly ContextWindowConfig _config;
        private readonly TokenEstimator _tokenEstimator;
        private readonly MessageSummarizer _summarizer;

        public ContextWindowManager(ContextWindowConfig config = null)
        {
            _config = config ?? new ContextWindowConfig();
            _tokenEstimator = new TokenEstimator();
            _summarizer = new MessageSummarizer();
        }

        // Replace the switch expression with a traditional switch statement to ensure compatibility with C# 7.3.
        public List<ContextAwareChatMessage> ManageContextWindow(List<ContextAwareChatMessage> messages)
        {
            // Update token estimates for all messages
            UpdateTokenEstimates(messages);

            var totalTokens = CalculateTotalTokens(messages);

            if (totalTokens <= _config.TargetTokens)
            {
                return messages; // No trimming needed
            }

            switch (_config.Strategy)
            {
                case TrimmingStrategy.TruncateOldest:
                    return TruncateOldest(messages);
                case TrimmingStrategy.TruncateMiddle:
                    return TruncateMiddle(messages);
                case TrimmingStrategy.Summarize:
                    return SummarizeOldMessages(messages);
                case TrimmingStrategy.SlidingWindow:
                    return ApplySlidingWindow(messages);
                case TrimmingStrategy.Importance:
                    return TrimByImportance(messages);
                case TrimmingStrategy.Hybrid:
                    return ApplyHybridStrategy(messages);
                default:
                    return TruncateOldest(messages);
            }
        }

        private void UpdateTokenEstimates(List<ContextAwareChatMessage> messages)
        {
            foreach (var message in messages.Where(m => m.EstimatedTokens == 0))
            {
                message.EstimatedTokens = _tokenEstimator.EstimateTokens(message.Content, _config.EstimationMethod);
            }
        }

        private int CalculateTotalTokens(List<ContextAwareChatMessage> messages)
        {
            return messages.Sum(m => m.EstimatedTokens);
        }

        private List<ContextAwareChatMessage> TruncateOldest(List<ContextAwareChatMessage> messages)
        {
            var result = new List<ContextAwareChatMessage>();
            var tokenCount = 0;

            // Always preserve system messages if configured
            if (_config.PreserveSystemMessages)
            {
                var systemMessages = messages.Where(m => m.IsSystemMessage).ToList();
                result.AddRange(systemMessages);
                tokenCount = systemMessages.Sum(m => m.EstimatedTokens);
            }

            // Add messages from newest to oldest until we hit the limit
            var nonSystemMessages = messages.Where(m => !m.IsSystemMessage || !_config.PreserveSystemMessages)
                                          .Reverse()
                                          .ToList();

            foreach (var message in nonSystemMessages)
            {
                if (tokenCount + message.EstimatedTokens <= _config.TargetTokens)
                {
                    result.Insert(_config.PreserveSystemMessages ? result.Count(m => m.IsSystemMessage) : 0, message);
                    tokenCount += message.EstimatedTokens;
                }
                else
                {
                    break;
                }
            }

            return result.OrderBy(m => m.Timestamp).ToList();
        }

        private List<ContextAwareChatMessage> TruncateMiddle(List<ContextAwareChatMessage> messages)
        {
            var result = new List<ContextAwareChatMessage>();
            var tokenCount = 0;

            // Keep system messages
            var systemMessages = messages.Where(m => m.IsSystemMessage).ToList();
            result.AddRange(systemMessages);
            tokenCount = systemMessages.Sum(m => m.EstimatedTokens);

            var nonSystemMessages = messages.Where(m => !m.IsSystemMessage).ToList();
            var remainingTokens = _config.TargetTokens - tokenCount;

            // Keep recent messages (last 30% of available tokens)
            var recentTokenBudget = (int)(remainingTokens * 0.7);
            var recentMessages = new List<ContextAwareChatMessage>();

            for (int i = nonSystemMessages.Count - 1; i >= 0; i--)
            {
                var message = nonSystemMessages[i];
                if (recentTokenBudget >= message.EstimatedTokens)
                {
                    recentMessages.Insert(0, message);
                    recentTokenBudget -= message.EstimatedTokens;
                }
                else
                {
                    break;
                }
            }

            // Keep earliest messages with remaining budget
            var earlyTokenBudget = remainingTokens - recentMessages.Sum(m => m.EstimatedTokens);
            var earlyMessages = new List<ContextAwareChatMessage>();

            foreach (var message in nonSystemMessages)
            {
                if (recentMessages.Contains(message)) break;

                if (earlyTokenBudget >= message.EstimatedTokens)
                {
                    earlyMessages.Add(message);
                    earlyTokenBudget -= message.EstimatedTokens;
                }
            }

            result.AddRange(earlyMessages);
            result.AddRange(recentMessages);

            return result.OrderBy(m => m.Timestamp).ToList();
        }

        private List<ContextAwareChatMessage> SummarizeOldMessages(List<ContextAwareChatMessage> messages)
        {
            var totalTokens = CalculateTotalTokens(messages);
            var tokensToRemove = totalTokens - _config.TargetTokens;

            if (tokensToRemove <= 0) return messages;

            var result = new List<ContextAwareChatMessage>();
            var messagesToSummarize = new List<ContextAwareChatMessage>();
            var recentMessages = new List<ContextAwareChatMessage>();

            var tokenCount = 0;

            // Keep recent messages
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                var message = messages[i];
                if (tokenCount + message.EstimatedTokens <= _config.TargetTokens * 0.6) // Keep 60% for recent
                {
                    recentMessages.Insert(0, message);
                    tokenCount += message.EstimatedTokens;
                }
                else
                {
                    if (message.CanBeSummarized && !message.IsPinned && !message.IsSystemMessage)
                    {
                        messagesToSummarize.Insert(0, message);
                    }
                    else
                    {
                        result.Insert(0, message);
                    }
                }
            }

            // Create summary if we have messages to summarize
            if (messagesToSummarize.Any())
            {
                var summaryTokenBudget = _config.TargetTokens - tokenCount - result.Sum(m => m.EstimatedTokens);
                var summaryText = _summarizer.SummarizeMessages(messagesToSummarize, summaryTokenBudget);

                var summaryMessage = new ContextAwareChatMessage(MessageRole.System,
                    $"[Summary of {messagesToSummarize.Count} previous messages]: {summaryText}")
                {
                    Priority = MessagePriority.High,
                    CanBeSummarized = false
                };

                summaryMessage.EstimatedTokens = _tokenEstimator.EstimateTokens(summaryMessage.Content, _config.EstimationMethod);
                result.Add(summaryMessage);
            }

            result.AddRange(recentMessages);
            return result.OrderBy(m => m.Timestamp).ToList();
        }

        private List<ContextAwareChatMessage> ApplySlidingWindow(List<ContextAwareChatMessage> messages)
        {
            var systemMessages = messages.Where(m => m.IsSystemMessage).ToList();
            var nonSystemMessages = messages.Where(m => !m.IsSystemMessage).ToList();

            var messagesToKeep = Math.Min(_config.MaxMessagesToKeep, nonSystemMessages.Count);
            // Replace the usage of TakeLast with a compatible approach for older C# versions
            var recentMessages = nonSystemMessages.Skip(Math.Max(0, nonSystemMessages.Count - messagesToKeep)).ToList();

            var result = new List<ContextAwareChatMessage>();
            result.AddRange(systemMessages);
            result.AddRange(recentMessages);

            return result.OrderBy(m => m.Timestamp).ToList();
        }

        private List<ContextAwareChatMessage> TrimByImportance(List<ContextAwareChatMessage> messages)
        {
            var sortedMessages = messages.OrderByDescending(m => GetMessageImportanceScore(m)).ToList();
            var result = new List<ContextAwareChatMessage>();
            var tokenCount = 0;

            foreach (var message in sortedMessages)
            {
                if (tokenCount + message.EstimatedTokens <= _config.TargetTokens)
                {
                    result.Add(message);
                    tokenCount += message.EstimatedTokens;
                }
            }

            return result.OrderBy(m => m.Timestamp).ToList();
        }

        private double GetMessageImportanceScore(ContextAwareChatMessage message)
        {
            double score = (int)message.Priority;

            if (message.IsSystemMessage) score += 10;
            if (message.IsPinned) score += 5;

            // Recent messages get bonus
            var age = DateTime.UtcNow - message.Timestamp;
            if (age.TotalMinutes < 30) score += 2;
            else if (age.TotalHours < 2) score += 1;

            return score;
        }

        private List<ContextAwareChatMessage> ApplyHybridStrategy(List<ContextAwareChatMessage> messages)
        {
            var totalTokens = CalculateTotalTokens(messages);

            if (totalTokens > _config.MaxTokens * 1.5)
            {
                // Aggressive trimming needed - use sliding window first
                messages = ApplySlidingWindow(messages);
            }

            if (CalculateTotalTokens(messages) > _config.TargetTokens)
            {
                // Still too large - try summarization
                messages = SummarizeOldMessages(messages);
            }

            if (CalculateTotalTokens(messages) > _config.TargetTokens)
            {
                // Last resort - truncate by importance
                messages = TrimByImportance(messages);
            }

            return messages;
        }

        public ContextWindowStats GetContextWindowStats(List<ContextAwareChatMessage> messages)
        {
            UpdateTokenEstimates(messages);

            return new ContextWindowStats
            {
                TotalMessages = messages.Count,
                TotalTokens = CalculateTotalTokens(messages),
                MaxTokens = _config.MaxTokens,
                TargetTokens = _config.TargetTokens,
                UtilizationPercentage = (double)CalculateTotalTokens(messages) / _config.MaxTokens * 100,
                SystemMessages = messages.Count(m => m.IsSystemMessage),
                PinnedMessages = messages.Count(m => m.IsPinned),
                AverageTokensPerMessage = messages.Any() ? (double)CalculateTotalTokens(messages) / messages.Count : 0,
                EstimationMethod = _config.EstimationMethod,
                TrimmingStrategy = _config.Strategy
            };
        }
    }
}