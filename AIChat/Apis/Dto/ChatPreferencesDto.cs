using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class ChatPreferencesDto
    {
        [JsonProperty("selectedMode")]
        public string SelectedMode { get; set; }

        [JsonProperty("selectedRule")]
        public string SelectedRule { get; set; }
    }
}
