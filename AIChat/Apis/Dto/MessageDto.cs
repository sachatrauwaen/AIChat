using Newtonsoft.Json;
using System.Collections.Generic;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class MessageDto
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("toolName")]
        public string ToolName { get; set; }

        [JsonProperty("toolFullname")]
        public string ToolFullname { get;  set; }

        public List<ContentDto> AContent { get; set; }

        [JsonProperty("inputTokens")]
        public int InputTokens { get; set; }

        [JsonProperty("outputTokens")]
        public int OutputTokens { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("cacheCreationInputTokens")]
        public int CacheCreationInputTokens { get; set; }

        [JsonProperty("cacheReadInputTokens")]
        public int CacheReadInputTokens { get; set; }
    }
}