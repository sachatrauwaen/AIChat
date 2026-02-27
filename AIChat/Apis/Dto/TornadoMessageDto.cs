using System.Collections.Generic;
using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class TornadoMessageDto
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("toolCallId")]
        public string ToolCallId { get; set; }

        [JsonProperty("toolName")]
        public string ToolName { get; set; }

        [JsonProperty("toolArguments")]
        public Dictionary<string, object> ToolArguments { get; set; }

        [JsonProperty("toolResult")]
        public string ToolResult { get; set; }

        [JsonProperty("inputTokens")]
        public int InputTokens { get; set; }

        [JsonProperty("outputTokens")]
        public int OutputTokens { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }
    }
}

