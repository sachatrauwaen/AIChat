using System;
using System.Collections.Generic;

namespace Satrabel.AIChat.History
{
    /// <summary>
    /// Builds a token- and turn-limited slice of a conversation
    /// to be used as context when calling the model.
    /// </summary>
    public class ChatHistoryTrimmer
    {
        /// <summary>
        /// Builds a list of messages that satisfy the given history policy.
        /// Messages are always returned in chronological order.
        /// </summary>
        public IReadOnlyList<ChatMessage> BuildContextMessages(
            ChatConversation conversation,
            ChatHistoryPolicy policy,
            out ChatHistoryUsage usage)
        {
            usage = new ChatHistoryUsage();

            if (conversation == null || conversation.Messages == null || conversation.Messages.Count == 0)
            {
                return Array.Empty<ChatMessage>();
            }

            if (policy == null)
            {
                policy = ChatHistoryPolicy.CreateDefault();
            }

            var effectiveMaxTokens = policy.GetEffectiveMaxContextTokens();
            var effectiveMaxUserTurns = policy.GetEffectiveMaxUserTurns();
            var minMessagesToKeep = policy.GetEffectiveMinMessagesToKeep();
            var toolWeight = policy.ToolTokenWeight > 0 ? policy.ToolTokenWeight : 1.0;

            usage.TotalMessages = conversation.Messages.Count;

            var kept = new List<ChatMessage>();
            var runningTokens = 0;
            var runningUserTurns = 0;

            // Walk from newest to oldest so we always keep the most recent context.
            for (var index = conversation.Messages.Count - 1; index >= 0; index--)
            {
                var message = conversation.Messages[index] ?? new ChatMessage();
                var approxTokens = EstimateTokens(message, toolWeight);
                var isUser = message.Role == MessageRole.User;
                var isTool = message.Role == MessageRole.Tool;
                var isToolWrapper = ChatHistoryHelpers.IsToolResultWrapper(message);

                // Track totals (for stats) regardless of whether we eventually keep the message.
                usage.TotalApproxTokens += approxTokens;
                IncrementTotals(usage, message.Role);

                // Tool messages and their synthetic wrapper messages are kept for UI
                // but should not consume context budget or appear in the replayed context.
                if (isTool || isToolWrapper)
                {
                    continue;
                }

                var wouldExceedTokens = runningTokens + approxTokens > effectiveMaxTokens && kept.Count >= minMessagesToKeep;
                var wouldExceedTurns = isUser &&
                                       (runningUserTurns + 1) > effectiveMaxUserTurns &&
                                       kept.Count >= minMessagesToKeep;

                if (wouldExceedTokens || wouldExceedTurns)
                {
                    continue;
                }

                kept.Add(message);
                runningTokens += approxTokens;
                if (isUser)
                {
                    runningUserTurns++;
                }

                usage.KeptApproxTokens += approxTokens;
                usage.KeptMessages++;
                IncrementKept(usage, message.Role);
            }

            kept.Reverse();
            return kept;
        }

        private static int EstimateTokens(ChatMessage message, double toolWeight)
        {
            var content = message?.Content ?? string.Empty;
            var baseTokens = Math.Max(1, content.Length / 4);

            if (message != null && message.Role == MessageRole.Tool)
            {
                baseTokens = (int)Math.Max(1, Math.Round(baseTokens * toolWeight));
            }

            return baseTokens;
        }

        private static void IncrementTotals(ChatHistoryUsage usage, MessageRole role)
        {
            switch (role)
            {
                case MessageRole.User:
                    usage.TotalUserMessages++;
                    break;
                case MessageRole.Assistant:
                    usage.TotalAssistantMessages++;
                    break;
                case MessageRole.System:
                    usage.TotalSystemMessages++;
                    break;
                case MessageRole.Tool:
                    usage.TotalToolMessages++;
                    break;
            }
        }

        private static void IncrementKept(ChatHistoryUsage usage, MessageRole role)
        {
            switch (role)
            {
                case MessageRole.User:
                    usage.KeptUserMessages++;
                    break;
                case MessageRole.Assistant:
                    usage.KeptAssistantMessages++;
                    break;
                case MessageRole.System:
                    usage.KeptSystemMessages++;
                    break;
                case MessageRole.Tool:
                    usage.KeptToolMessages++;
                    break;
            }
        }
    }
}

