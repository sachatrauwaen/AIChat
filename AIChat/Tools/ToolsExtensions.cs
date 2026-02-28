using Microsoft.Extensions.DependencyInjection;
using Satrabel.AIChat.Tools;

namespace Dnn.Mcp.WebApi.Tools
{
    public static class ToolsExtensions
    {
        public static void RegisterTools(this IServiceCollection services)
        {
            services.AddTransient<IMcpProvider, AddPageTool>();
            services.AddTransient<IMcpProvider, UpdatePageTool>();
            services.AddTransient<IMcpProvider, GetPagesTool>();
            services.AddTransient<IMcpProvider, GetPageTool>();

            services.AddTransient<IMcpProvider, AddModuleTool>();
            services.AddTransient<IMcpProvider, GetModulesTool>();

            services.AddTransient<IMcpProvider, GetFoldersTool>();
            services.AddTransient<IMcpProvider, GetFilesTool>();
            services.AddTransient<IMcpProvider, ReadFileTool>();
            services.AddTransient<IMcpProvider, WriteFileTool>();

            services.AddTransient<IMcpProvider, GetSystemFilesTool>();
            services.AddTransient<IMcpProvider, ReadSystemFileTool>();
            services.AddTransient<IMcpProvider, WriteSystemFileTool>();

            services.AddTransient<IMcpProvider, GetHtmlTool>();
            // GetUrlSeoTool moved to SeoTools project
            services.AddTransient<IMcpProvider, GetHtmlModuleTool>();
            services.AddTransient<IMcpProvider, UpdateHtmlModuleTool>();
            services.AddTransient<IMcpProvider, SendEmailTool>();

        }
    }
}
