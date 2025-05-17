using AnthropicClient.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Satrabel.PersonaBar.AIChat.Apis.Dto
{
    public class ContentDto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        
        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("input")]
        public Dictionary<string, object> Input { get; set; }

    }
}