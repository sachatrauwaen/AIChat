using Satrabel.AIChat.History;
using System;
using System.Collections.Generic;

namespace Satrabel.AIChat.ContextWindow
{
    // Example usage
    public class ContextWindowExample
    {
        public static void RunExample()
        {
            var config = new ContextWindowConfig
            {
                MaxTokens = 4000,
                TargetTokens = 3500,
                Strategy = TrimmingStrategy.Hybrid,
                EstimationMethod = TokenEstimationMethod.GPT
            };

            var contextManager = new ContextWindowManager(config);
            var messages = new List<ContextAwareChatMessage>();

            // Add system message
            messages.Add(new ContextAwareChatMessage(MessageRole.System,
                "You are a helpful AI assistant.", MessagePriority.System));

            // Simulate a long conversation
            for (int i = 0; i < 50; i++)
            {
                messages.Add(new ContextAwareChatMessage(MessageRole.User,
                    $"This is user message {i}. Can you help me with something important?"));
                messages.Add(new ContextAwareChatMessage(MessageRole.Assistant,
                    $"Of course! I'd be happy to help you with message {i}. Here's a detailed response..."));
            }

            Console.WriteLine("Before context management:");
            var stats = contextManager.GetContextWindowStats(messages);
            Console.WriteLine($"Messages: {stats.TotalMessages}, Tokens: {stats.TotalTokens}");

            // Apply context window management
            var managedMessages = contextManager.ManageContextWindow(messages);

            Console.WriteLine("After context management:");
            var newStats = contextManager.GetContextWindowStats(managedMessages);
            Console.WriteLine($"Messages: {newStats.TotalMessages}, Tokens: {newStats.TotalTokens}");
            Console.WriteLine($"Utilization: {newStats.UtilizationPercentage:F1}%");
        }
    }
}