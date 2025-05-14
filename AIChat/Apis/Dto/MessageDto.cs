using Newtonsoft.Json;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class MessageDto
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("toolName")]
        public string ToolName { get; set; }

        [JsonProperty("toolFullname")]
        public string ToolFullname { get;  set; }
    }
}