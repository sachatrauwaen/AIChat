using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Satrabel.AIChat.History
{

    // Main chat history manager class
    public class ChatHistoryManager
    {
        private readonly string _dataDirectory;
        private readonly Dictionary<string, ChatConversation> _conversations;
        private readonly JsonSerializerSettings _jsonOptions;

        public ChatHistoryManager(string dataDirectory = "ChatHistory")
        {
            _dataDirectory = dataDirectory;
            _conversations = new Dictionary<string, ChatConversation>();
            _jsonOptions = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                
            };

            Directory.CreateDirectory(_dataDirectory);
        }

        // Create a new conversation
        public ChatConversation CreateConversation(string title = null)
        {
            var conversation = new ChatConversation
            {
                Title = title ?? $"Chat {DateTime.Now:yyyy-MM-dd HH:mm}"
            };

            _conversations[conversation.Id] = conversation;
            return conversation;
        }

        // Get conversation by ID
        public ChatConversation GetConversation(string conversationId)
        {
            _conversations.TryGetValue(conversationId, out var conversation);
            return conversation;
        }

        // Get all conversations
        public List<ChatConversation> GetAllConversations()
        {
            return _conversations.Values.OrderByDescending(c => c.LastModified).ToList();
        }

        // Add message to conversation
        public void AddMessage(string conversationId, MessageRole role, string content)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation.AddMessage(role, content);
            }
        }

        // Delete conversation
        public bool DeleteConversation(string conversationId)
        {
            if (_conversations.Remove(conversationId))
            {
                var filePath = Path.Combine(_dataDirectory, $"{conversationId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            return false;
        }

        // Save conversation to file
        public void SaveConversation(string conversationId)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                var filePath = Path.Combine(_dataDirectory, $"{conversationId}.json");
                var json = JsonConvert.SerializeObject(conversation, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
        }

        // Load conversation from file
        public ChatConversation LoadConversation(string conversationId)
        {
            var filePath = Path.Combine(_dataDirectory, $"{conversationId}.json");

            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                var conversation = JsonConvert.DeserializeObject<ChatConversation>(json, _jsonOptions);

                if (conversation != null)
                {
                    _conversations[conversation.Id] = conversation;
                }

                return conversation;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading conversation {conversationId}: {ex.Message}");
                return null;
            }
        }

        // Save all conversations
        public void SaveAllConversationsA()
        {
            foreach (var item in _conversations.Keys)
            {
                SaveConversation(item);
            }
        }

        // Load all conversations from directory
        public void LoadAllConversationsAsync()
        {
            var files = Directory.GetFiles(_dataDirectory, "*.json");

            foreach (var file in files)
            {
                var conversationId = Path.GetFileNameWithoutExtension(file);
                LoadConversation(conversationId);
            }
        }

        // Search messages across all conversations
        public List<(ChatConversation Conversation, ChatMessage Message)> SearchMessages(
            string searchTerm,
            MessageRole? role = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var results = new List<(ChatConversation, ChatMessage)>();

            foreach (var conversation in _conversations.Values)
            {
                var matchingMessages = conversation.Messages.Where(m =>
                    m.Content.Contains(searchTerm) &&
                    (role == null || m.Role == role) &&
                    (fromDate == null || m.Timestamp >= fromDate) &&
                    (toDate == null || m.Timestamp <= toDate));

                foreach (var message in matchingMessages)
                {
                    results.Add((conversation, message));
                }
            }

            return results.OrderByDescending(r => r.Item2.Timestamp).ToList();
        }

        // Get conversation statistics
        public ConversationStats GetConversationStats(string conversationId)
        {
            if (!_conversations.TryGetValue(conversationId, out var conversation))
                return new ConversationStats();

            return new ConversationStats
            {
                TotalMessages = conversation.Messages.Count,
                UserMessages = conversation.Messages.Count(m => m.Role == MessageRole.User),
                AssistantMessages = conversation.Messages.Count(m => m.Role == MessageRole.Assistant),
                SystemMessages = conversation.Messages.Count(m => m.Role == MessageRole.System),
                EstimatedTokens = conversation.GetTokenCount(),
                FirstMessageTime = conversation.Messages.FirstOrDefault()?.Timestamp,
                LastMessageTime = conversation.Messages.LastOrDefault()?.Timestamp,
                ConversationDuration = conversation.Messages.Any() ?
                    conversation.Messages.Last().Timestamp - conversation.Messages.First().Timestamp :
                    TimeSpan.Zero
            };
        }

        // Replace the switch expression with a traditional switch statement to ensure compatibility with C# 7.3
        public string ExportConversation(string conversationId, ExportFormat format)
        {
            if (!_conversations.TryGetValue(conversationId, out var conversation))
                throw new ArgumentException("Conversation not found");

            switch (format)
            {
                case ExportFormat.Json:
                    return JsonConvert.SerializeObject(conversation, _jsonOptions);
                case ExportFormat.Text:
                    return ExportToText(conversation);
                case ExportFormat.Markdown:
                    return ExportToMarkdown(conversation);
                default:
                    throw new ArgumentException("Unsupported export format");
            }
        }

        private string ExportToText(ChatConversation conversation)
        {
            var lines = new List<string>
            {
                $"Conversation: {conversation.Title}",
                $"Created: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}",
                $"Last Modified: {conversation.LastModified:yyyy-MM-dd HH:mm:ss}",
                new string('=', 50)
            };

            foreach (var message in conversation.Messages)
            {
                lines.Add($"[{message.Timestamp:HH:mm:ss}] {message.Role}: {message.Content}");
                lines.Add("");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportToMarkdown(ChatConversation conversation)
        {
            var lines = new List<string>
            {
                $"# {conversation.Title}",
                "",
                $"**Created:** {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}",
                $"**Last Modified:** {conversation.LastModified:yyyy-MM-dd HH:mm:ss}",
                "",
                "---",
                ""
            };

            foreach (var message in conversation.Messages)
            {
                string roleEmoji;
                switch (message.Role)
                {
                    case MessageRole.User:
                        roleEmoji = "👤";
                        break;
                    case MessageRole.Assistant:
                        roleEmoji = "🤖";
                        break;
                    case MessageRole.System:
                        roleEmoji = "⚙️";
                        break;
                    default:
                        roleEmoji = "💬";
                        break;
                }

                lines.Add($"## {roleEmoji} {message.Role} - {message.Timestamp:HH:mm:ss}");
                lines.Add("");
                lines.Add(message.Content);
                lines.Add("");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}