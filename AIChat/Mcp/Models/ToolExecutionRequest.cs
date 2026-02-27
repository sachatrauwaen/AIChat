using System.Collections.Generic;

namespace Dnn.Mcp.WebApi.Models
{
    /// <summary>
    /// Represents a request to execute a tool or list tools.
    /// </summary>
    public class ToolExecutionRequest
    {
        /// <summary>
        /// Gets or sets the action type ("list" or "execute").
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool ID (required for execute action).
        /// </summary>
        public string? ToolId { get; set; }

        /// <summary>
        /// Gets or sets the execution parameters (required for execute action).
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
