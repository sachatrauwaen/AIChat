
using DotNetNuke.Web.Api;

namespace Dnn.Mcp.WebApi
{
    /// <summary>
    /// The ServiceRouteMapper tells the DNN Web API Framework what routes this module uses.
    /// Based on the pattern from Dnn.ContactList.Spa: https://github.com/dnnsoftware/Dnn.Platform/blob/develop/DNN%20Platform/Modules/Samples/Dnn.ContactList.Spa/Services/ServiceRouteMapper.cs
    /// </summary>
    public class ServiceRouteMapper : IServiceRouteMapper
    {
        /// <summary>
        /// RegisterRoutes is used to register the module's routes.
        /// Maps routes like: ~/DesktopModules/Dnn.Mcp.WebApi/API/McpTools/HandleRequest
        /// and ~/DesktopModules/Dnn.Mcp.WebApi/API/mcp for the MCP controller
        /// </summary>
        /// <param name="mapRouteManager">The route manager.</param>
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            // MCP endpoint route - single endpoint for POST, GET, DELETE (routed by HTTP verb)
            // Maps to: ~/DesktopModules/Dnn.Mcp.WebApi/API/mcp
            mapRouteManager.MapHttpRoute(
                moduleFolderName: "Dnn/McpWebApi",
                routeName: "mcp",
                url: "mcp",
                defaults: new { controller = "Mcp" },
                namespaces: new[] { "Dnn.Mcp.WebApi.Controllers.Mcp" });

        }
    }
}

