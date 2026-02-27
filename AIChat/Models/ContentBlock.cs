using Newtonsoft.Json;

namespace Dnn.Mcp.WebApi.Models
{
    public abstract class ContentBlock
    {
        [JsonProperty("type")]
        public abstract string Type { get; }
    }
}
