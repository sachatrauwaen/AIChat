using DotNetNuke.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Satrabel.OpenContentMcp.Tools;

namespace Satrabel.OpenContentMcp
{
    public class Startup : IDnnStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterOpenContentTools();
        }
    }
}
