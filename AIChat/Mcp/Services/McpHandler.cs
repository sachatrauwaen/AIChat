using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Models.Mcp;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Satrabel.PersonaBar.DnnMcp.Apis;

namespace Dnn.Mcp.WebApi.Services.Mcp
{
    /// <summary>
    /// Interface for MCP JSON-RPC message handler.
    /// </summary>
    public interface IMcpHandler
    {
        /// <summary>
        /// Processes a JSON-RPC request and returns the response.
        /// Returns null for notifications (no response needed).
        /// </summary>
        JsonRpcResponse HandleRequest(JsonRpcRequest request);
    }

    /// <summary>
    /// Processes MCP JSON-RPC methods and returns responses.
    /// Used by the controller to handle messages synchronously or via SSE.
    /// </summary>
    public class McpHandler : IMcpHandler
    {
        private readonly IMcpRegistry _mcpRegistry;             

        private readonly IServiceProvider _dependencyProvider;

        private const string ProtocolVersion = "2024-11-05";
        private const string ServerName = "McpStreamableHttpServer";
        private const string ServerVersion = "1.0.0";   
        private string _currentLogLevel = "info"; // Default log level

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public McpHandler(IMcpRegistry mcpRegistry, IServiceProvider dependencyProvider)
        {
            _mcpRegistry = mcpRegistry ?? throw new System.ArgumentNullException(nameof(mcpRegistry));
            _dependencyProvider = dependencyProvider ?? throw new System.ArgumentNullException(nameof(dependencyProvider));
        }

        /// <summary>
        /// Processes a JSON-RPC request and returns the response.
        /// Returns null for notifications (no response needed).
        /// </summary>
        public JsonRpcResponse HandleRequest(JsonRpcRequest request)
        {
            ModuleInitializer.Initialize(_mcpRegistry, _dependencyProvider);

            switch (request.Method)
            {
                case "initialize":
                    return HandleInitialize(request);

                case "notifications/initialized":
                    return null; // Notification, no response

                case "notifications/cancelled":
                    return null; // Cancellation notification

                case "ping":
                    return HandlePing(request);

                case "tools/list":
                    return HandleToolsList(request);

                case "tools/call":
                    return HandleToolsCall(request);

                case "resources/list":
                    return HandleResourcesList(request);

                case "resources/read":
                    return HandleResourcesRead(request);

                case "resources/templates/list":
                    return HandleResourceTemplatesList(request);

                case "prompts/list":
                    return HandlePromptsList(request);

                case "prompts/get":
                    return HandlePromptsGet(request);

                case "logging/setLevel":
                    return HandleLoggingSetLevel(request);

                default:
                    return JsonRpcResponse.ErrorResponse(
                        request.Id,
                        JsonRpcErrorCodes.MethodNotFound,
                        $"Method not found: {request.Method}");
            }
        }

        private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
        {
            var result = new InitializeResult
            {
                ProtocolVersion = ProtocolVersion,
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability { ListChanged = false },
                    Resources = new { }, // Indicate resources support
                    Prompts = new { },   // Indicate prompts support
                    Logging = new { }    // Indicate logging support
                },
                ServerInfo = new Implementation
                {
                    Name = ServerName,
                    Version = ServerVersion
                }
            };
            return JsonRpcResponse.Success(request.Id, result);
        }

        private JsonRpcResponse HandlePing(JsonRpcRequest request)
        {
            return JsonRpcResponse.Success(request.Id, new PingResult());
        }

        private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
        {
            var tools = _mcpRegistry.GetAllTools();
            var portalSettings = PortalSettings.Current;
            var toolsList = PortalController.GetPortalSetting(DnnMcpController.TOOLS_SETTING, portalSettings.PortalId, "").Split(',').ToList();

            var toolDefinitions = tools
                .Where(t => toolsList.Contains(t.Name))
                .Select(t => new Models.Mcp.McpToolDefinition
                {
                    Name = t.Name,
                    Description = t.Description,
                    InputSchema = ConvertParametersToJsonSchema(t.Parameters)
                }).ToList();

            var result = new ToolsListResult
            {
                //Tools = new List<ToolDefinition> { CalculatorTool.GetDefinition() }
                Tools = toolDefinitions
            };
            return JsonRpcResponse.Success(request.Id, result);
        }

        /// <summary>
        /// Converts DNN tool parameters to MCP JSON Schema format.
        /// </summary>
        /// <param name="parameters">The DNN tool parameters.</param>
        /// <returns>JSON schema element.</returns>
        private JObject ConvertParametersToJsonSchema(List<ToolParameter> parameters)
        {
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject()
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

        private JsonRpcResponse HandleToolsCall(JsonRpcRequest request)
        {
            if (request.Params == null)
            {
                return JsonRpcResponse.ErrorResponse(
                    request.Id, JsonRpcErrorCodes.InvalidParams,
                    "Missing params for tools/call");
            }

            var toolCallParams = request.Params.ToObject<ToolCallParams>();
            if (string.IsNullOrEmpty(toolCallParams?.Name))
            {
                return JsonRpcResponse.ErrorResponse(
                    request.Id, JsonRpcErrorCodes.InvalidParams,
                    "Missing tool name in params");
            }

            var portalSettings = PortalSettings.Current;
            var toolsList = PortalController.GetPortalSetting(DnnMcpController.TOOLS_SETTING, portalSettings.PortalId, "").Split(',').ToList();

            if (!toolsList.Contains(toolCallParams.Name))
            {
                return JsonRpcResponse.ErrorResponse(
                    request.Id, JsonRpcErrorCodes.InvalidParams,
                    $"Tool not found: {toolCallParams.Name}");
            }

            var parameters = new System.Collections.Generic.Dictionary<string, object>();
            if (toolCallParams.Arguments != null)
            {
                foreach (var kvp in toolCallParams.Arguments)
                {
                    parameters[kvp.Key] = kvp.Value.ToObject<object>();
                }
            }

            var tool = _mcpRegistry.GetTool(toolCallParams.Name);

            if (tool == null) { 

                return JsonRpcResponse.ErrorResponse(
                    request.Id, JsonRpcErrorCodes.InvalidParams,
                    $"Tool not found: {toolCallParams.Name}");
            }

            // Check required parameters
            foreach (var param in tool.Parameters.Where(p => p.Required))
            {
                if (!parameters.ContainsKey(param.Name))
                {
                    return JsonRpcResponse.ErrorResponse(
                        request.Id, JsonRpcErrorCodes.InvalidParams,
                        $"Missing required parameter: {param.Name}");
                }
            }

            // Execute tool
            if (tool.Handler == null)
            {
                return JsonRpcResponse.ErrorResponse(
                    request.Id, JsonRpcErrorCodes.InvalidParams,
                    "Tool handler is not configured");
            }
            var result = tool.Handler(parameters);

            return JsonRpcResponse.Success(request.Id, result);
        }

        // ═══════════════════════════════════════════════════════════════
        // RESOURCES HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private JsonRpcResponse HandleResourcesList(JsonRpcRequest request)
        {
            var resources = _mcpRegistry.GetAllResources();

            var resourceInfos = resources.Select(r => new ResourceInfo
            {
                Uri = r.Uri,
                Name = r.Name,
                Description = r.Description,
                MimeType = r.MimeType
            }).ToArray();

            var result = new ResourceListResult
            {
                Resources = resourceInfos
            };

            return JsonRpcResponse.Success(request.Id, result);
        }

        private JsonRpcResponse HandleResourcesRead(JsonRpcRequest request)
        {
            try
            {
                var paramsObj = request.Params as JObject;
                var uri = paramsObj?["uri"]?.ToString();

                if (string.IsNullOrEmpty(uri))
                {
                    return JsonRpcResponse.ErrorResponse(
                        request.Id,
                        JsonRpcErrorCodes.InvalidParams,
                        "Missing resource URI");
                }

                var resource = _mcpRegistry.GetResource(uri);
                if (resource == null)
                {
                    return JsonRpcResponse.ErrorResponse(
                        request.Id,
                        JsonRpcErrorCodes.InvalidParams,
                        $"Resource not found: {uri}");
                }

                // Execute resource handler
                if (resource.Handler == null)
                {
                    return JsonRpcResponse.ErrorResponse(
                        request.Id,
                        JsonRpcErrorCodes.InvalidParams,
                        "Resource handler is not configured");
                }

                var parameters = new Dictionary<string, object> { ["uri"] = uri };
                var handlerResult = resource.Handler(parameters);

                // Convert handler result to ResourceReadResult
                var result = new ResourceReadResult
                {
                    Contents = new[]
                    {
                        new ResourceContent
                        {
                            Uri = uri,
                            MimeType = resource.MimeType,
                            Text = handlerResult?.ToString() ?? string.Empty
                        }
                    }
                };

                return JsonRpcResponse.Success(request.Id, result);
            }
            catch (System.ArgumentException ex)
            {
                return JsonRpcResponse.ErrorResponse(
                    request.Id,
                    JsonRpcErrorCodes.InvalidParams,
                    ex.Message);
            }
            catch (System.Exception ex)
            {
                return JsonRpcResponse.ErrorResponse(
                    request.Id,
                    JsonRpcErrorCodes.InternalError,
                    $"Resource read failed: {ex.Message}");
            }
        }

        private JsonRpcResponse HandleResourceTemplatesList(JsonRpcRequest request)
        {
            var resources = _mcpRegistry.GetAllResources();

            var templates = resources
                .Where(r => r.IsTemplate && !string.IsNullOrEmpty(r.UriTemplate))
                .Select(r => new ResourceTemplate
                {
                    UriTemplate = r.UriTemplate,
                    Name = r.Name,
                    Description = r.Description,
                    MimeType = r.MimeType
                }).ToArray();

            var result = new ResourceTemplateListResult
            {
                ResourceTemplates = templates
            };

            return JsonRpcResponse.Success(request.Id, result);
        }

        // ═══════════════════════════════════════════════════════════════
        // PROMPTS HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private JsonRpcResponse HandlePromptsList(JsonRpcRequest request)
        {
            var prompts = _mcpRegistry.GetAllPrompts();

            var promptInfos = prompts.Select(p => new PromptInfo
            {
                Name = p.Name,
                Description = p.Description,
                Arguments = p.Parameters.Select(param => new PromptArgument
                {
                    Name = param.Name,
                    Description = param.Description,
                    Required = param.Required
                }).ToArray()
            }).ToArray();

            var result = new PromptListResult
            {
                Prompts = promptInfos
            };

            return JsonRpcResponse.Success(request.Id, result);
        }

        private JsonRpcResponse HandlePromptsGet(JsonRpcRequest request)
        {
            try
            {
                var paramsObj = request.Params as JObject;
                var name = paramsObj?["name"]?.ToString();
                var arguments = paramsObj?["arguments"] as JObject;

                if (string.IsNullOrEmpty(name))
                {
                    return JsonRpcResponse.ErrorResponse(
                        request.Id,
                        JsonRpcErrorCodes.InvalidParams,
                        "Missing prompt name");
                }

                var prompt = _mcpRegistry.GetPrompt(name);
                if (prompt == null)
                {
                    return JsonRpcResponse.ErrorResponse(
                        request.Id,
                        JsonRpcErrorCodes.InvalidParams,
                        $"Prompt not found: {name}");
                }

                // Convert arguments to dictionary
                var parameters = new Dictionary<string, object>();
                if (arguments != null)
                {
                    foreach (var kvp in arguments)
                    {
                        parameters[kvp.Key] = kvp.Value.ToObject<object>();
                    }
                }

                // Check required parameters
                foreach (var param in prompt.Parameters.Where(p => p.Required))
                {
                    if (!parameters.ContainsKey(param.Name))
                    {
                        return JsonRpcResponse.ErrorResponse(
                            request.Id,
                            JsonRpcErrorCodes.InvalidParams,
                            $"Missing required argument: {param.Name}");
                    }
                }

                // Execute prompt handler
                if (prompt.Handler == null)
                {
                    return JsonRpcResponse.ErrorResponse(
                        request.Id,
                        JsonRpcErrorCodes.InvalidParams,
                        "Prompt handler is not configured");
                }

                var result = prompt.Handler(parameters);

                return JsonRpcResponse.Success(request.Id, result);
            }
            catch (System.ArgumentException ex)
            {
                return JsonRpcResponse.ErrorResponse(
                    request.Id,
                    JsonRpcErrorCodes.InvalidParams,
                    ex.Message);
            }
            catch (System.Exception ex)
            {
                return JsonRpcResponse.ErrorResponse(
                    request.Id,
                    JsonRpcErrorCodes.InternalError,
                    $"Prompt execution failed: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LOGGING HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private JsonRpcResponse HandleLoggingSetLevel(JsonRpcRequest request)
        {
            try
            {
                var paramsObj = request.Params as JObject;
                var level = paramsObj?["level"]?.ToString();

                if (string.IsNullOrEmpty(level))
                {
                    // This is a notification, but we can still log the error
                    Trace.WriteLine("logging/setLevel: Missing level parameter");
                    return null; // Notification, no response
                }

                // Validate log level (MCP spec: debug, info, notice, warning, error, critical, alert, emergency)
                var validLevels = new[] { "debug", "info", "notice", "warning", "error", "critical", "alert", "emergency" };
                if (!validLevels.Contains(level.ToLowerInvariant()))
                {
                    Trace.WriteLine($"logging/setLevel: Invalid log level '{level}'");
                    return null; // Notification, no response
                }

                _currentLogLevel = level.ToLowerInvariant();
                Trace.WriteLine($"Log level changed to: {_currentLogLevel}");

                return null; // Notification, no response needed
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine($"logging/setLevel error: {ex.Message}");
                return null; // Notification, no response even on error
            }
        }
    }
}
