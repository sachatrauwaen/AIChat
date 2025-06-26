using Newtonsoft.Json;
using AnthropicClient.Models;


namespace Satrabel.PersonaBar.AIChat.Apis
{
    public partial class AIChatController
    {
        public class ChatToolRequest : ChatRequest
        {
            [JsonProperty("toolChoice")]
            public string ToolChoice { get; set; }

            [JsonProperty("disableParallelToolUse")]
            public bool? DisableParallelToolUse { get; set; }


            [JsonProperty("runTool")]
            public bool RunTool { get; set; }

            [JsonProperty("toolUse")]
            public ToolUseContent ToolUse { get; set; }

            [JsonProperty("mode")]
            public string Mode { get; set; }

            [JsonProperty("rules")]
            public string Rules { get; set; }
        }
    }
}