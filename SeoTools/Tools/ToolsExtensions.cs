using Dnn.Mcp.WebApi;
using Microsoft.Extensions.DependencyInjection;

namespace Satrabel.SeoTools.Tools
{
    public static class ToolsExtensions
    {
        public static void RegisterSeoTools(this IServiceCollection services)
        {
            services.AddTransient<IMcpProvider, GetUrlSeoTool>();
        }
    }
}
