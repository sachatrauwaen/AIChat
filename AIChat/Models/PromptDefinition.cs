using System;
using System.Collections.Generic;
using Dnn.Mcp.WebApi.Models.Mcp;

namespace Dnn.Mcp.WebApi.Models
{
    /// <summary>
    /// Represents a prompt definition with its metadata and execution handler.
    /// </summary>
    public class PromptDefinition
    {
        /// <summary>
        /// Gets or sets the unique prompt name/identifier.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///

        public string Title { get; set; }= string.Empty;

        /// <summary>
        /// Gets or sets the prompt description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt arguments/parameters.
        /// </summary>
        public List<PromptParameter> Parameters { get; set; } = new List<PromptParameter>();

        /// <summary>
        /// Gets or sets the prompt template or content.
        /// </summary>
        public string Template { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt execution handler.
        /// </summary>
        public Func<Dictionary<string, object>, PromptGetResult>? Handler { get; set; }

        /// <summary>
        /// Gets or sets the optional prompt category.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets whether the prompt is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
       
    }

    /// <summary>
    /// Represents a parameter for a prompt.
    /// </summary>
    public class PromptParameter
    {
        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the parameter is required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the default value for the parameter.
        /// </summary>
        public object? DefaultValue { get; set; }
    }
}
