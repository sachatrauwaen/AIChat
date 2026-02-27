using System.Collections.Generic;
using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class TornadoToolCallDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments")]
        public Dictionary<string, object> Arguments { get; set; }

        [JsonProperty("readOnly")]
        public bool ReadOnly { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}

