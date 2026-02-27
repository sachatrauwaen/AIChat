using System;
using Dnn.Mcp.WebApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dnn.Mcp.WebApi
{
    /// <summary>
    /// Initializes the MCP Web API module.
    /// </summary>
    public static class ModuleInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the module by registering sample tools.
        /// </summary>
        public static void Initialize(IMcpRegistry registry, IServiceProvider dependencyProvider)
        {
            lock (_lock)
            {
                if (_initialized)
                {
                    return;
                }

                // Register sample tools
                //SampleTools.RegisterSampleTools(registry);
                var providers = dependencyProvider.GetServices<IMcpProvider>();
                foreach (var provider in providers)
                {
                    provider.Register(registry);
                }

                _initialized = true;
            }
        }
    }
}
