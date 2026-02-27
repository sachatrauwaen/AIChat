using Newtonsoft.Json;
using System.Collections.Generic;

namespace Satrabel.PersonaBar.DnnMcp.Apis
{
    public class SettingsDto
    {
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        [JsonProperty("apiKeyValidDelay")]
        public int ApiKeyValidDelay { get; set; }

        [JsonProperty("apiKeyValidDelayActive")]
        public bool ApiKeyValidDelayActive { get; set; }

        [JsonProperty("apiKeyValidUntilDate")]
        public string ApiKeyValidUntilDate { get; set; }

        [JsonProperty("globalRules")]
        public string GlobalRules { get; set; }

        [JsonProperty("rules")]
        public List<RuleDto> Rules { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("tools")]
        public List<ToolDto> Tools { get; set; }
    }
}