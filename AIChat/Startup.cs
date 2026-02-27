using Dnn.Mcp.WebApi.Prompts;
using Dnn.Mcp.WebApi.Resources;
using Dnn.Mcp.WebApi.Services;
using Dnn.Mcp.WebApi.Services.Mcp;
using Dnn.Mcp.WebApi.Tools;
using DotNetNuke.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Satrabel.AIChat.Tools;

namespace Dnn.Mcp.WebApi
{
    public class Startup : IDnnStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterPrompts();
            services.RegisterResources();
            services.RegisterTools();
            services.AddTransient<IMcpHandler, McpHandler>();
            services.AddSingleton<IMcpRegistry, McpRegistry>();
        }
    }
}
