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

        [JsonProperty("autoReadonlyTools")]
        public bool AutoReadonlyTools { get; set; }

        [JsonProperty("autoWriteTools")]
        public bool AutoWriteTools { get; set; }

        [JsonProperty("selectedMode")]
        public string SelectedMode { get; set; }

        [JsonProperty("selectedRule")]
        public string SelectedRule { get; set; }

        [JsonProperty("debug")]
        public bool Debug { get; set; }
    }
}