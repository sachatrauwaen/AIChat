using System;
using System.Collections.Generic;

namespace Dnn.Mcp.WebApi.Models
{
    /// <summary>
    /// Represents a resource definition with its metadata and content handler.
    /// </summary>
    public class ResourceDefinition
    {
        /// <summary>
        /// Gets or sets the unique resource URI.
        /// </summary>
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable resource name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MIME type of the resource.
        /// </summary>
        public string MimeType { get; set; } = "text/plain";

        /// <summary>
        /// Gets or sets the resource content handler.
        /// </summary>
        public Func<Dictionary<string, object>, object>? Handler { get; set; }

        /// <summary>
        /// Gets or sets the optional resource category.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets whether the resource is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this is a template resource (dynamic URI).
        /// </summary>
        public bool IsTemplate { get; set; }

        /// <summary>
        /// Gets or sets the URI template for dynamic resources.
        /// </summary>
        public string? UriTemplate { get; set; }
    }
}
