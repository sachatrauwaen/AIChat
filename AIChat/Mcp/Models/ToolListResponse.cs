using System.Collections.Generic;
using System.Linq;

namespace Dnn.Mcp.WebApi.Models
{
    /// <summary>
    /// Represents a response containing a list of available tools.
    /// </summary>
    public class ToolListResponse
    {
        /// <summary>
        /// Gets or sets the list of tool definitions.
        /// </summary>
        public List<ToolDefinitionDto> Tools { get; set; } = new List<ToolDefinitionDto>();

        /// <summary>
        /// Gets the total count of tools.
        /// </summary>
        public int Count => Tools.Count;
    }

    /// <summary>
    /// Data transfer object for tool definition (without handler).
    /// </summary>
    public class ToolDefinitionDto
    {
        /// <summary>
        /// Gets or sets the unique tool identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable tool name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool parameters.
        /// </summary>
        public List<ToolParameter> Parameters { get; set; } = new List<ToolParameter>();

        /// <summary>
        /// Gets or sets the optional tool category.
        /// </summary>
        public string? Category { get; set; }
    }
}
