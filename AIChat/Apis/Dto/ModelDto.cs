using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class ModelDto
    {
        [JsonProperty("name")]
        public string Name { get; internal set; }

        [JsonProperty("value")]
        public string Value { get; internal set; }
    }
}