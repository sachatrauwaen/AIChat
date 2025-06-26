using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class RuleDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rule")]
        public string Rule { get; set; }
    }
}