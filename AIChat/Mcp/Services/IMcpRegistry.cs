using System.Collections.Generic;
using Dnn.Mcp.WebApi.Models;

namespace Dnn.Mcp.WebApi.Services
{
    /// <summary>
    /// Interface for managing MCP (Model Context Protocol) registrations including tools, prompts, and resources.
    /// </summary>
    public interface IMcpRegistry
    {
        // ─── Tools ────────────────────────────────────────────────────
        
        /// <summary>
        /// Registers a tool in the registry.
        /// </summary>
        /// <param name="tool">The tool definition to register.</param>
        void RegisterTool(ToolDefinition tool);

        /// <summary>
        /// Gets all registered tools.
        /// </summary>
        /// <returns>A collection of all registered tools.</returns>
        IEnumerable<ToolDefinition> GetAllTools();

        /// <summary>
        /// Gets a specific tool by its name.
        /// </summary>
        /// <param name="toolName">The tool name.</param>
        /// <returns>The tool definition, or null if not found.</returns>
        ToolDefinition? GetTool(string toolName);

        /// <summary>
        /// Checks if a tool is registered.
        /// </summary>
        /// <param name="toolName">The tool name.</param>
        /// <returns>True if the tool exists, false otherwise.</returns>
        bool ToolExists(string toolName);

        // ─── Prompts ──────────────────────────────────────────────────
        
        /// <summary>
        /// Registers a prompt in the registry.
        /// </summary>
        /// <param name="prompt">The prompt definition to register.</param>
        void RegisterPrompt(PromptDefinition prompt);

        /// <summary>
        /// Gets all registered prompts.
        /// </summary>
        /// <returns>A collection of all registered prompts.</returns>
        IEnumerable<PromptDefinition> GetAllPrompts();

        /// <summary>
        /// Gets a specific prompt by its name.
        /// </summary>
        /// <param name="promptName">The prompt name.</param>
        /// <returns>The prompt definition, or null if not found.</returns>
        PromptDefinition? GetPrompt(string promptName);

        /// <summary>
        /// Checks if a prompt is registered.
        /// </summary>
        /// <param name="promptName">The prompt name.</param>
        /// <returns>True if the prompt exists, false otherwise.</returns>
        bool PromptExists(string promptName);

        // ─── Resources ────────────────────────────────────────────────
        
        /// <summary>
        /// Registers a resource in the registry.
        /// </summary>
        /// <param name="resource">The resource definition to register.</param>
        void RegisterResource(ResourceDefinition resource);

        /// <summary>
        /// Gets all registered resources.
        /// </summary>
        /// <returns>A collection of all registered resources.</returns>
        IEnumerable<ResourceDefinition> GetAllResources();

        /// <summary>
        /// Gets a specific resource by its URI.
        /// </summary>
        /// <param name="resourceUri">The resource URI.</param>
        /// <returns>The resource definition, or null if not found.</returns>
        ResourceDefinition? GetResource(string resourceUri);

        /// <summary>
        /// Checks if a resource is registered.
        /// </summary>
        /// <param name="resourceUri">The resource URI.</param>
        /// <returns>True if the resource exists, false otherwise.</returns>
        bool ResourceExists(string resourceUri);
    }
}
