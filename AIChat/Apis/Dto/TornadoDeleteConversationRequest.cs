using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class TornadoDeleteConversationRequest
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}

