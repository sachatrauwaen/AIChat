using DotNetNuke.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Satrabel.SeoTools.Tools;

namespace Satrabel.SeoTools
{
    public class Startup : IDnnStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterSeoTools();
        }
    }
}
