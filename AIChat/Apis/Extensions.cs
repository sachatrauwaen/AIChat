using AnthropicClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satrabel.AIChat.Apis
{
    public static class Extensions
    {
        public static string ToText(this Content content)
        {
            switch (content)
            {
                case TextContent textContent:
                    return textContent.Text;
                    break;
                case ToolUseContent toolUseContent:
                    return $"{toolUseContent.Name} ({string.Join(", ", toolUseContent.Input.Select(d=> d.Key+ "="+d.Value))})";
                    break;
                case ToolResultContent toolResultContent:
                    return $"{toolResultContent.Content}";
                    break;
            }
            return string.Empty;
        }

    }
}
