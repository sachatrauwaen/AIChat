using Microsoft.Extensions.DependencyInjection;


namespace Dnn.Mcp.WebApi.Prompts
{
    public static class PromptsExtensions
    {
        public static void RegisterPrompts(this IServiceCollection services)
        {
            services.AddTransient<IMcpProvider, PromptProvider>();
            services.AddTransient<IMcpProvider, RulesPromptProvider>();
        }
    }
}
