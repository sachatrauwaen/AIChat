using System;
using System.Collections.Generic;

namespace Dnn.Mcp.WebApi.Models
{
    /// <summary>
    /// Represents a tool definition with its metadata and execution handler.
    /// </summary>
    public class ToolDefinition
    {
        /// <summary>
        /// Gets or sets the unique tool name/identifier.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool parameters.
        /// </summary>
        public List<ToolParameter> Parameters { get; set; } = new List<ToolParameter>();

        /// <summary>
        /// Gets or sets the tool execution handler.
        /// </summary>
        public Func<Dictionary<string, object>, CallToolResult>? Handler { get; set; }

        /// <summary>
        /// Gets or sets the optional tool category.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets whether the tool is read only.
        /// </summary>
        public bool ReadOnly { get; set; } = false;
    }
}
