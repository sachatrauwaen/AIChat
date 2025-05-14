using System.Collections.Generic;

namespace Satrabel.PersonaBar.AIChat.Services
{
    internal class MessageParameters
    {
        public List<Message> Messages { get; set; }
        public int MaxTokens { get; set; }
        public object Model { get; set; }
        public bool Stream { get; set; }
        public decimal Temperature { get; set; }
    }
}