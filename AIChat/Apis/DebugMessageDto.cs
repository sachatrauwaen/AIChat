using System.Collections.Generic;

namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class DebugMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public string ToolCallId { get; set; }
        public List<string> ToolCalls { get; set; }
        public int MessageTokens { get; set; }
    }
}