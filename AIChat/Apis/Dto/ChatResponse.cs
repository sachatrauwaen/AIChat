using System.Collections.Generic;
using Newtonsoft.Json;
using Satrabel.PersonaBar.AIChat.Apis.Dto;


namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class ChatResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("messages")]
        public IEnumerable<MessageDto> Messages { get; set; }

        //[JsonProperty("aMessages")]
        //public List<Message> AMessages { get; set; }

        //[JsonProperty("aResponse")]
        //public MessageResponse AResponse { get; set; }

        [JsonProperty("tool")]
        public ToolResponse Tool { get; set; }

        [JsonProperty("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonProperty("totalInputTokens")]
        public int TotalInputTokens { get; set; }

        [JsonProperty("totalOutputTokens")]
        public int TotalOutputTokens { get; set; }
        public int TotalCacheCreationInputTokens { get; set; }
        public int TotalCacheReadInputTokens { get; set; }
    }
}