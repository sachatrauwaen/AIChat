using Newtonsoft.Json;
using System.Collections.Generic;

namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class SettingsDto
    {
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        [JsonProperty("globalRules")]
        public string GlobalRules { get; set; }

        [JsonProperty("rules")]
        public List<RuleDto> Rules { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("models")]
        public List<ModelDto> Models { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("tools")]
        public List<ToolDto> Tools { get; set; }

        [JsonProperty("maxTokens")]
        public int MaxTokens { get; set; }

        [JsonProperty("autoReadonlyTools")]
        public bool AutoReadonlyTools { get; set; }

        [JsonProperty("autoWriteTools")]
        public bool AutoWriteTools { get; set; }
    }
}