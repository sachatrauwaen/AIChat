using AnthropicClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satrabel.AIChat.Services
{
    public interface IAIChatTool : ITool
    {
        bool ReadOnly { get; }
    }
}
