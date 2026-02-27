namespace Dnn.Mcp.WebApi.Models
{
    /// <summary>
    /// Represents the result of a tool execution.
    /// </summary>
    public class ToolExecutionResult
    {
        /// <summary>
        /// Gets or sets whether the execution succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the execution result data.
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Gets or sets the error message (if failed).
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTime { get; set; }
    }
}
