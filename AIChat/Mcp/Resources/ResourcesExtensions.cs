using Microsoft.Extensions.DependencyInjection;

namespace Dnn.Mcp.WebApi.Resources
{
    public static class ResourcesExtensions
    {
        public static void RegisterResources(this IServiceCollection services)
        {
            services.AddTransient<IMcpProvider, ResourceProvider>();
        }
    }
}
