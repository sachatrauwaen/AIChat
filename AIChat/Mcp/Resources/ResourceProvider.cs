using System.Collections.Generic;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using Newtonsoft.Json;

namespace Dnn.Mcp.WebApi.Resources
{
    public class ResourceProvider : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            // Example resource: DNN configuration information
            registry.RegisterResource(new ResourceDefinition
            {
                Uri = "dnn://config/site-info",
                Name = "DNN Site Information",
                Description = "Provides general information about the DNN site configuration",
                MimeType = "application/json",
                Category = "configuration",
                IsEnabled = true,
                Handler = (arguments) =>
                {
                    var siteInfo = new
                    {
                        platform = "DotNetNuke (DNN)",
                        version = "9.x",
                        description = "DNN Platform is a web content management system and web application framework",
                        capabilities = new[]
                        {
                            "Page Management",
                            "Module Management",
                            "File Management",
                            "User Management",
                            "Content Management"
                        }
                    };

                    return new
                    {
                        uri = "dnn://config/site-info",
                        mimeType = "application/json",
                        text = JsonConvert.SerializeObject(siteInfo, Formatting.Indented)
                    };
                }
            });
        }     
    }
}
