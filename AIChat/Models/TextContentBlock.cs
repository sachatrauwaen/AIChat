using Newtonsoft.Json;

namespace Dnn.Mcp.WebApi.Models
{
    public class TextContentBlock : ContentBlock
    {
        public override string Type => "text";

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override string ToString() => Text ?? "";
    }
}
