using System.Collections.Generic;
using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class TornadoChatResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }

        [JsonProperty("messages")]
        public IEnumerable<TornadoMessageDto> Messages { get; set; }

        [JsonProperty("toolCall")]
        public TornadoToolCallDto ToolCall { get; set; }

        [JsonProperty("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonProperty("totalInputTokens")]
        public int TotalInputTokens { get; set; }

        [JsonProperty("totalOutputTokens")]
        public int TotalOutputTokens { get; set; }
    }
}

