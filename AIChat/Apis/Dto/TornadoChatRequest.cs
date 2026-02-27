using System.Collections.Generic;
using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class TornadoChatRequest
    {
        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("runTool")]
        public bool RunTool { get; set; }

        [JsonProperty("toolCallId")]
        public string ToolCallId { get; set; }

        [JsonProperty("toolName")]
        public string ToolName { get; set; }

        [JsonProperty("toolArguments")]
        public Dictionary<string, object> ToolArguments { get; set; }

        [JsonProperty("toolApproved")]
        public bool ToolApproved { get; set; } = true;

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("rules")]
        public string Rules { get; set; }
    }
}

