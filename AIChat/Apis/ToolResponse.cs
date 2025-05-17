using AnthropicClient.Models;
using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class ToolResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fullname")]
        public string Fullname { get; set; }

        [JsonProperty("toolUse")]
        public ToolUseContent ToolUse { get; set; }
    }
}