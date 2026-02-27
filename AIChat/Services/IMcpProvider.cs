using Dnn.Mcp.WebApi.Services;

namespace Dnn.Mcp.WebApi
{
    /// <summary>
    /// Interface for MCP providers that can register tools with the MCP registry.
    /// </summary>
    public interface IMcpProvider
    {
        /// <summary>
        /// Registers tools with the MCP registry.
        /// </summary>
        /// <param name="registry">The MCP registry to register tools with.</param>
        void Register(IMcpRegistry registry);
    }
}
