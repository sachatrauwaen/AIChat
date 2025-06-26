using System.Collections.Generic;
using Newtonsoft.Json;
using AnthropicClient.Models;
using Satrabel.PersonaBar.AIChat.Apis.Dto;


namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class ChatRequest
    {
        [JsonProperty("messages")]
        public List<MessageDto> Messages { get; set; }

    }
}