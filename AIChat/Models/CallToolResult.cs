using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dnn.Mcp.WebApi.Models
{
    public class CallToolResult
    {
        [JsonProperty("content")]
        public List<ContentBlock> Content { get; set; } = new List<ContentBlock>();

        [JsonProperty("isError")]
        public bool IsError  { get; set; }
    }
}
