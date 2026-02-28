using Dnn.Mcp.WebApi;
using Microsoft.Extensions.DependencyInjection;

namespace Satrabel.OpenContentMcp.Tools
{
    public static class ToolsExtensions
    {
        public static void RegisterOpenContentTools(this IServiceCollection services)
        {
            services.AddTransient<IMcpProvider, ManageTemplatesTool>();
            services.AddTransient<IMcpProvider, AddOpenContentTool>();
            services.AddTransient<IMcpProvider, GetOpenContentTool>();
            services.AddTransient<IMcpProvider, UpdateOpenContentTool>();
        }
    }
}

