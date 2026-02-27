using Newtonsoft.Json;
using System.Collections.Generic;

namespace Satrabel.PersonaBar.DnnMcp.Apis
{
    public class InfoDto
    {
        [JsonProperty("rules")]
        public List<string> Rules { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}