using System;
using System.IO;
using System.Threading.Tasks;

namespace Satrabel.AIChat.History
{
    // Example usage class
    public class Program
    {
        public static void Main(string[] args)
        {
            var historyManager = new ChatHistoryManager();

            // Create a new conversation
            var conversation = historyManager.CreateConversation("My AI Chat");

            // Add some messages
            historyManager.AddMessage(conversation.Id, MessageRole.User, "Hello, how are you?");
            historyManager.AddMessage(conversation.Id, MessageRole.Assistant, "I'm doing well, thank you! How can I help you today?");
            historyManager.AddMessage(conversation.Id, MessageRole.User, "Can you explain quantum computing?");
            historyManager.AddMessage(conversation.Id, MessageRole.Assistant, "Quantum computing is a type of computation that harnesses quantum mechanical phenomena...");

            // Save the conversation
            historyManager.SaveConversation(conversation.Id);

            // Get conversation statistics
            var stats = historyManager.GetConversationStats(conversation.Id);
            Console.WriteLine($"Total messages: {stats.TotalMessages}");
            Console.WriteLine($"Estimated tokens: {stats.EstimatedTokens}");

            // Search for messages
            var searchResults = historyManager.SearchMessages("quantum", MessageRole.User);
            Console.WriteLine($"Found {searchResults.Count} matching messages");

            // Export conversation
            var markdown = historyManager.ExportConversation(conversation.Id, ExportFormat.Markdown);
            File.WriteAllText("conversation.md", markdown);

            Console.WriteLine("Chat history management demo completed!");
        }
    }
}