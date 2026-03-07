using Newtonsoft.Json;

namespace Satrabel.PersonaBar.DnnMcp.Apis
{
    public class ToolDto
    {
        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
    }
}