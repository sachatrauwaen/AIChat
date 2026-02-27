using Newtonsoft.Json;

namespace Satrabel.PersonaBar.DnnMcp.Apis
{
    public class RuleDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rule")]
        public string Rule { get; set; }
    }
}