using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dnn.Mcp.WebApi.Models;

namespace Dnn.Mcp.WebApi.Services
{
    /// <summary>
    /// Service for executing registered tools.
    /// </summary>
    public class DnnToolExecutor
    {
        private readonly IMcpRegistry _mcpRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="DnnToolExecutor"/> class.
        /// </summary>
        /// <param name="mcpRegistry">The MCP registry.</param>
        public DnnToolExecutor(IMcpRegistry mcpRegistry)
        {
            _mcpRegistry = mcpRegistry ?? throw new ArgumentNullException(nameof(mcpRegistry));
        }

        /// <summary>
        /// Executes a tool with the given parameters.
        /// </summary>
        /// <param name="toolId">The tool identifier.</param>
        /// <param name="parameters">The execution parameters.</param>
        /// <returns>The execution result.</returns>
        public async Task<ToolExecutionResult> ExecuteAsync(string toolId, Dictionary<string, object> parameters)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate tool ID
                if (string.IsNullOrWhiteSpace(toolId))
                {
                    return new ToolExecutionResult
                    {
                        Success = false,
                        Error = "Tool ID is required",
                        ExecutionTime = stopwatch.ElapsedMilliseconds
                    };
                }

                // Get tool from registry
                var tool = _mcpRegistry.GetTool(toolId);
                if (tool == null)
                {
                    stopwatch.Stop();
                    return new ToolExecutionResult
                    {
                        Success = false,
                        Error = $"Tool not found: {toolId}",
                        ExecutionTime = stopwatch.ElapsedMilliseconds
                    };
                }

                // Validate parameters
                if (parameters == null)
                {
                    parameters = new Dictionary<string, object>();
                }

                // Check required parameters
                var missingParams = tool.Parameters
                    .Where(p => p.Required && !parameters.ContainsKey(p.Name))
                    .Select(p => p.Name)
                    .ToList();

                if (missingParams.Any())
                {
                    stopwatch.Stop();
                    return new ToolExecutionResult
                    {
                        Success = false,
                        Error = $"Missing required parameters: {string.Join(", ", missingParams)}",
                        ExecutionTime = stopwatch.ElapsedMilliseconds
                    };
                }

                // Validate parameter types (basic validation)
                foreach (var param in tool.Parameters)
                {
                    if (parameters.ContainsKey(param.Name))
                    {
                        var value = parameters[param.Name];
                        if (!ValidateParameterType(value, param.Type))
                        {
                            stopwatch.Stop();
                            return new ToolExecutionResult
                            {
                                Success = false,
                                Error = $"Parameter '{param.Name}' has invalid type. Expected: {param.Type}",
                                ExecutionTime = stopwatch.ElapsedMilliseconds
                            };
                        }
                    }
                }

                // Execute tool
                if (tool.Handler == null)
                {
                    stopwatch.Stop();
                    return new ToolExecutionResult
                    {
                        Success = false,
                        Error = "Tool handler is not configured",
                        ExecutionTime = stopwatch.ElapsedMilliseconds
                    };
                }

                var executionResult = tool.Handler(parameters);
                stopwatch.Stop();

                return new ToolExecutionResult
                {
                    Success = true,
                    Result = executionResult,
                    ExecutionTime = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Tool execution failed: {ex.Message}",
                    ExecutionTime = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Validates that a parameter value matches the expected type.
        /// </summary>
        /// <param name="value">The parameter value.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private bool ValidateParameterType(object value, string expectedType)
        {
            if (value == null)
            {
                return true; // Null is allowed for optional parameters
            }

            return expectedType.ToLowerInvariant() switch
            {
                "string" => value is string,
                "int" => value is int || value is long || value is short,
                "float" => value is float || value is double || value is decimal,
                "bool" => value is bool,
                "array" => value is System.Collections.IEnumerable,
                "object" => value is Dictionary<string, object> || value.GetType().IsClass,
                _ => true // Unknown types are allowed
            };
        }
    }
}
