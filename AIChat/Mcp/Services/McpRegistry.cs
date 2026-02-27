using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dnn.Mcp.WebApi.Models;

namespace Dnn.Mcp.WebApi.Services
{
    /// <summary>
    /// Thread-safe in-memory implementation of the MCP registry for tools, prompts, and resources.
    /// </summary>
    public class McpRegistry : IMcpRegistry
    {
        private readonly ConcurrentDictionary<string, ToolDefinition> _tools = new ConcurrentDictionary<string, ToolDefinition>();
        private readonly ConcurrentDictionary<string, PromptDefinition> _prompts = new ConcurrentDictionary<string, PromptDefinition>();
        private readonly ConcurrentDictionary<string, ResourceDefinition> _resources = new ConcurrentDictionary<string, ResourceDefinition>();

        public McpRegistry(/*IEnumerable<IMcpProvider> mcpProviders = null*/)
        {
            // Register tools, prompts, and resources
            //if (mcpProviders != null)
            //{

            //    foreach (var mcpProvider in mcpProviders)
            //    {
            //        try
            //        {
            //            mcpProvider.Register(this);
            //        }
            //        catch 
            //        {

            //        }
                    
            //    }
            //}
        }

        // ─── Tools ────────────────────────────────────────────────────

        /// <summary>
        /// Registers a tool in the registry.
        /// </summary>
        /// <param name="tool">The tool definition to register.</param>
        public void RegisterTool(ToolDefinition tool)
        {
            if (tool == null)
            {
                throw new System.ArgumentNullException(nameof(tool));
            }

            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                throw new System.ArgumentException("Tool name cannot be empty", nameof(tool));
            }

            _tools[tool.Name] = tool;
        }

        /// <summary>
        /// Gets all registered tools.
        /// </summary>
        /// <returns>A collection of all registered tools.</returns>
        public IEnumerable<ToolDefinition> GetAllTools()
        {
            return _tools.Values.ToList();
        }

        /// <summary>
        /// Gets a specific tool by its name.
        /// </summary>
        /// <param name="toolName">The tool name.</param>
        /// <returns>The tool definition, or null if not found.</returns>
        public ToolDefinition? GetTool(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                return null;
            }

            _tools.TryGetValue(toolName, out var tool);
            return tool;
        }

        /// <summary>
        /// Checks if a tool is registered.
        /// </summary>
        /// <param name="toolName">The tool name.</param>
        /// <returns>True if the tool exists, false otherwise.</returns>
        public bool ToolExists(string toolName)
        {
            return GetTool(toolName) != null;
        }

        // ─── Prompts ──────────────────────────────────────────────────

        /// <summary>
        /// Registers a prompt in the registry.
        /// </summary>
        /// <param name="prompt">The prompt definition to register.</param>
        public void RegisterPrompt(PromptDefinition prompt)
        {
            if (prompt == null)
            {
                throw new System.ArgumentNullException(nameof(prompt));
            }

            if (string.IsNullOrWhiteSpace(prompt.Name))
            {
                throw new System.ArgumentException("Prompt name cannot be empty", nameof(prompt));
            }

            _prompts[prompt.Name] = prompt;
        }

        /// <summary>
        /// Gets all registered prompts.
        /// </summary>
        /// <returns>A collection of all registered prompts.</returns>
        public IEnumerable<PromptDefinition> GetAllPrompts()
        {
            return _prompts.Values.Where(p => p.IsEnabled).ToList();
        }

        /// <summary>
        /// Gets a specific prompt by its name.
        /// </summary>
        /// <param name="promptName">The prompt name.</param>
        /// <returns>The prompt definition, or null if not found.</returns>
        public PromptDefinition? GetPrompt(string promptName)
        {
            if (string.IsNullOrWhiteSpace(promptName))
            {
                return null;
            }

            _prompts.TryGetValue(promptName, out var prompt);
            return prompt?.IsEnabled == true ? prompt : null;
        }

        /// <summary>
        /// Checks if a prompt is registered.
        /// </summary>
        /// <param name="promptName">The prompt name.</param>
        /// <returns>True if the prompt exists, false otherwise.</returns>
        public bool PromptExists(string promptName)
        {
            return GetPrompt(promptName) != null;
        }

        // ─── Resources ────────────────────────────────────────────────

        /// <summary>
        /// Registers a resource in the registry.
        /// </summary>
        /// <param name="resource">The resource definition to register.</param>
        public void RegisterResource(ResourceDefinition resource)
        {
            if (resource == null)
            {
                throw new System.ArgumentNullException(nameof(resource));
            }

            if (string.IsNullOrWhiteSpace(resource.Uri))
            {
                throw new System.ArgumentException("Resource URI cannot be empty", nameof(resource));
            }

            _resources[resource.Uri] = resource;
        }

        /// <summary>
        /// Gets all registered resources.
        /// </summary>
        /// <returns>A collection of all registered resources.</returns>
        public IEnumerable<ResourceDefinition> GetAllResources()
        {
            return _resources.Values.Where(r => r.IsEnabled).ToList();
        }

        /// <summary>
        /// Gets a specific resource by its URI.
        /// </summary>
        /// <param name="resourceUri">The resource URI.</param>
        /// <returns>The resource definition, or null if not found.</returns>
        public ResourceDefinition? GetResource(string resourceUri)
        {
            if (string.IsNullOrWhiteSpace(resourceUri))
            {
                return null;
            }

            _resources.TryGetValue(resourceUri, out var resource);
            return resource?.IsEnabled == true ? resource : null;
        }

        /// <summary>
        /// Checks if a resource is registered.
        /// </summary>
        /// <param name="resourceUri">The resource URI.</param>
        /// <returns>True if the resource exists, false otherwise.</returns>
        public bool ResourceExists(string resourceUri)
        {
            return GetResource(resourceUri) != null;
        }
    }
}
