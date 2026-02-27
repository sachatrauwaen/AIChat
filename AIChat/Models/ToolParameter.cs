using System;

namespace Dnn.Mcp.WebApi.Models
{
    /// <summary>
    /// Represents a parameter definition for a tool.
    /// </summary>
    public class ToolParameter
    {
        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter type (string, int, float, bool, array, object).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether the parameter is required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the default value for the parameter.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the minimum value (for numeric types).
        /// </summary>
        public int? MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value (for numeric types).
        /// </summary>
        public int? MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the allowed enum values.
        /// </summary>
        public string[]? EnumValues { get; set; }
    }
}
