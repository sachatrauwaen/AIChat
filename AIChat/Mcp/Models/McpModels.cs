using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dnn.Mcp.WebApi.Models.Mcp
{
    // ─── Initialize ───────────────────────────────────────────────

    public class InitializeParams
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("capabilities")]
        public ClientCapabilities Capabilities { get; set; }

        [JsonProperty("clientInfo")]
        public Implementation ClientInfo { get; set; }
    }

    public class ClientCapabilities
    {
        [JsonProperty("roots", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Roots { get; set; }

        [JsonProperty("sampling", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Sampling { get; set; }
    }

    public class InitializeResult
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("capabilities")]
        public ServerCapabilities Capabilities { get; set; }

        [JsonProperty("serverInfo")]
        public Implementation ServerInfo { get; set; }
    }

    public class ServerCapabilities
    {
        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public ToolsCapability Tools { get; set; }

        [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
        public object Resources { get; set; }

        [JsonProperty("prompts", NullValueHandling = NullValueHandling.Ignore)]
        public object Prompts { get; set; }

        [JsonProperty("logging", NullValueHandling = NullValueHandling.Ignore)]
        public object Logging { get; set; }
    }

    public class ToolsCapability
    {
        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; }
    }

    public class Implementation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    // ─── Tools ────────────────────────────────────────────────────

    public class ToolsListResult
    {
        [JsonProperty("tools")]
        public List<McpToolDefinition> Tools { get; set; } = new List<McpToolDefinition>();
    }

    public class McpToolDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("inputSchema")]
        public JObject InputSchema { get; set; }
    }

    public class ToolCallParams
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments")]
        public JObject Arguments { get; set; }
    }

    public class ToolCallResult
    {
        [JsonProperty("content")]
        public List<ToolContent> Content { get; set; } = new List<ToolContent>();

        [JsonProperty("isError", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsError { get; set; }
    }

    public class ToolContent
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    // ─── Ping ─────────────────────────────────────────────────────

    public class PingResult
    {
        // Empty object per MCP spec
    }

    // ═══════════════════════════════════════════════════════════════
    // RESOURCES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Result for resources/list request.
    /// </summary>
    public class ResourceListResult
    {
        [JsonProperty("resources")]
        public ResourceInfo[] Resources { get; set; }
    }

    /// <summary>
    /// Information about a resource.
    /// </summary>
    public class ResourceInfo
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }

    /// <summary>
    /// Result for resources/read request.
    /// </summary>
    public class ResourceReadResult
    {
        [JsonProperty("contents")]
        public ResourceContent[] Contents { get; set; }
    }

    /// <summary>
    /// Content of a resource.
    /// </summary>
    public class ResourceContent
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    /// <summary>
    /// Result for resources/templates/list request.
    /// </summary>
    public class ResourceTemplateListResult
    {
        [JsonProperty("resourceTemplates")]
        public ResourceTemplate[] ResourceTemplates { get; set; }
    }

    /// <summary>
    /// Resource template for dynamic resources.
    /// </summary>
    public class ResourceTemplate
    {
        [JsonProperty("uriTemplate")]
        public string UriTemplate { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // PROMPTS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Result for prompts/list request.
    /// </summary>
    public class PromptListResult
    {
        [JsonProperty("prompts")]
        public PromptInfo[] Prompts { get; set; }
    }

    /// <summary>
    /// Information about a prompt.
    /// </summary>
    public class PromptInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("arguments")]
        public PromptArgument[] Arguments { get; set; }
    }

    /// <summary>
    /// Argument for a prompt template.
    /// </summary>
    public class PromptArgument
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }
    }

    /// <summary>
    /// Result for prompts/get request.
    /// </summary>
    public class PromptGetResult
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("messages")]
        public PromptMessage[] Messages { get; set; }
    }

    /// <summary>
    /// Message in a prompt.
    /// </summary>
    public class PromptMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public PromptContent Content { get; set; }
    }

    /// <summary>
    /// Content of a prompt message.
    /// </summary>
    public class PromptContent
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
