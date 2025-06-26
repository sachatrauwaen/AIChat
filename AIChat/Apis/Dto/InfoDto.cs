using Newtonsoft.Json;
using System.Collections.Generic;

namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class InfoDto
    {
        [JsonProperty("rules")]
        public List<string> Rules { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("models")]
        public List<ModelDto> Models { get; set; }
    }
}