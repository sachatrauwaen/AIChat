using AnthropicClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satrabel.AIChat.Services
{
    public class MyAutoToolChoice : AutoToolChoice
    {
        [Newtonsoft.Json.JsonProperty("disable_parallel_tool_use")]
        public bool DisableParallelToolUse { get; set; } = true;
    }
}
