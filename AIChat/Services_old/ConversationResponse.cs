using System.Collections.Generic;

namespace Satrabel.PersonaBar.AIChat.Services
{
    public class ConversationResponse
    {
        public List<Message> Messages { get; internal set; }

        public AnthropicResponse FinalResponse { get; set; }

    }
}