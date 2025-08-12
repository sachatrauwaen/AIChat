using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class ToolDto
    {
        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}