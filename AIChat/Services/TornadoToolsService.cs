using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Framework;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using LlmTornado.Infra;
using LlmTornado.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Xml;
using ModulesControllerLibrary = Dnn.PersonaBar.Library.Controllers.ModulesController;

namespace Satrabel.AIChat.Services
{
    internal class TornadoToolsService
    {
        private readonly List<Tool> _readOnlyTools;
        private readonly List<Tool> _writeTools;
        private readonly List<Tool> _allTools;
        private readonly HashSet<string> _readOnlyNames;

        private readonly IMcpRegistry _mcpRegistry;

        private readonly IServiceProvider _dependencyProvider;

        public TornadoToolsService(IMcpRegistry mcpRegistry, IServiceProvider dependencyProvider)
        {
            _mcpRegistry = mcpRegistry;
            _dependencyProvider = dependencyProvider;
            _readOnlyTools = BuildReadOnlyTools();
            _writeTools = BuildWriteTools();
            _allTools = new List<Tool>();
            _allTools.AddRange(_readOnlyTools);
            _allTools.AddRange(_writeTools);
            _readOnlyNames = new HashSet<string>(_readOnlyTools.Select(t => t.Function?.Name));
        }

        public List<Tool> GetReadOnlyTools() => _readOnlyTools;
        public List<Tool> GetWriteTools() => _writeTools;
        public List<Tool> GetAllTools() => _allTools;
        public bool IsReadOnly(string toolName) => _readOnlyNames.Contains(toolName);

        public async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> arguments)
        {
            try
            {
                var tool = _mcpRegistry.GetTool(toolName); // Ensure tool exists and user has permission
                if (tool != null)
                {
                    var result = tool.Handler(arguments ?? new Dictionary<string, object>()); // Let the MCP handler execute if available
                    return result != null ? JsonConvert.SerializeObject(result) : string.Empty;
                }
                return $"Unknown tool: {toolName}";
            }
            catch (Exception ex)
            {
                return $"Error executing tool {toolName}: {ex.Message}";
            }
        }

        #region Tool implementations

        #endregion

        #region Tool definitions

        private List<Tool> BuildReadOnlyTools()
        {
            return _mcpRegistry.GetAllTools().Where(t => t.ReadOnly == true).Select(t =>
                new Tool(new ToolFunction(
                    t.Name,
                    t.Description,
                    ConvertParametersToJsonSchema(t.Parameters)
                ), true)
             ).ToList();
        }

        [Description("Description")]
        //[ToolName("ToolName")] //optional attribute to override tool name
        string MyTool([Description("Description")] string param1, [Description("Description")] string param2)
        {
            // Tool implementation
            return $"Result: {param1} - {param2}";
        }

        private List<Tool> BuildWriteTools()
        {
            /*
            return new List<Tool>
            {
                new Tool(new List<ToolParam>
                {
                    new ToolParam("filePath", "The path of the file to write to", ToolParamAtomicTypes.String, true),
                    new ToolParam("content", "The content file to write to", ToolParamAtomicTypes.String, true),

                }, "write-file"), 
               
            };
            */

            return _mcpRegistry.GetAllTools().Where(t => t.ReadOnly == false).Select(t =>
                new Tool(new ToolFunction(
                    t.Name,
                    t.Description,
                    ConvertParametersToJsonSchema(t.Parameters)
                ), true)
             ).ToList();
        }

        #endregion

        #region Helpers

        private static long GetLong(Dictionary<string, object> args, string key)
        {
            if (args.TryGetValue(key, out var val) && val != null)
            {
                if (val is long l) return l;
                long.TryParse(val.ToString(), out var parsed);
                return parsed;
            }
            return 0;
        }

        private static string GetString(Dictionary<string, object> args, string key)
        {
            if (args.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return string.Empty;
        }

        private JObject ConvertParametersToJsonSchema(List<ToolParameter> parameters)
        {
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject(),
                ["additionalProperties"] = false
            };

            var properties = (JObject)schema["properties"]!;
            var required = new JArray();

            foreach (var param in parameters)
            {
                var propertySchema = new JObject
                {
                    ["type"] = MapParameterType(param.Type)
                };

                if (!string.IsNullOrWhiteSpace(param.Description))
                {
                    propertySchema["description"] = param.Description;
                }

                // Add numeric constraints
                if (param.MinValue.HasValue && (param.Type == "int" || param.Type == "float"))
                {
                    propertySchema["minimum"] = param.MinValue.Value;
                }

                if (param.MaxValue.HasValue && (param.Type == "int" || param.Type == "float"))
                {
                    propertySchema["maximum"] = param.MaxValue.Value;
                }

                // Add enum values
                if (param.EnumValues != null && param.EnumValues.Length > 0)
                {
                    var enumArray = new JArray();
                    foreach (var enumValue in param.EnumValues)
                    {
                        enumArray.Add(enumValue);
                    }
                    propertySchema["enum"] = enumArray;
                }

                properties[param.Name] = propertySchema;

                if (param.Required)
                {
                    required.Add(param.Name);
                }
            }

            if (required.Count > 0)
            {
                schema["required"] = required;
            }

            return schema;
        }
        /// <summary>
        /// Maps DNN parameter type to JSON Schema type.
        /// </summary>
        /// <param name="dnnType">The DNN parameter type.</param>
        /// <returns>The JSON Schema type.</returns>
        private string MapParameterType(string dnnType)
        {
            return dnnType.ToLowerInvariant() switch
            {
                "string" => "string",
                "int" => "integer",
                "float" => "number",
                "bool" => "boolean",
                "array" => "array",
                "object" => "object",
                _ => "string" // Default to string for unknown types
            };
        }

        #endregion
    }
}
