using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Modules.Html;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Satrabel.AIChat.Tools
{
    /// <summary>
    /// MCP tool to get HTML content from a DNN HTML module.
    /// </summary>
    public class GetHtmlModuleTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-html",
                Title = "Get HTML Module Content",
                Description = "Get the HTML content from a DNN HTML module",
                Parameters = new List<ToolParameter>
                {
                     new ToolParameter
                    {
                        Name = "tabId",
                        Description = "The ID of the page containing the module",
                        Required = true,
                        Type = "number"
                    },
                    new ToolParameter
                    {
                        Name = "moduleId",
                        Description = "The ID of the HTML module",
                        Required = true,
                        Type = "number"
                    }
                },
                Handler = (arguments) =>
                {
                    var moduleId = Convert.ToInt32(arguments["moduleId"]);
                    int tabId = Convert.ToInt32(arguments["tabId"]);

                    var result = GetHtml(moduleId, tabId);

                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock
                            {
                                Text = result
                            }
                        }
                    };
                }
            });
        }

        /// <summary>
        /// Gets the HTML content from a DNN HTML module using reflection (DotNetNuke.Modules.Html).
        /// </summary>
        public string GetHtml(int moduleId, int tabId)
        {
            try
            {
                var module = ModuleController.Instance.GetModule(moduleId, tabId, false);
                if (module == null)
                {
                    return JsonConvert.SerializeObject(new { error = "Module not found." });
                }

                if (!string.Equals(module.DesktopModule?.ModuleName, "HTML", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonConvert.SerializeObject(new { error = "Module is not an HTML module. Only HTML modules are supported." });
                }

                var controllerInstance = new DotNetNuke.Modules.Html.HtmlTextController();
                var workflowID = controllerInstance.GetWorkflow(moduleId, tabId, PortalSettings.Current.PortalId).Value;
                var htmlInfo = controllerInstance.GetTopHtmlText(moduleId, false, workflowID);

                if (htmlInfo == null)
                {
                    return JsonConvert.SerializeObject(new { content = "", moduleId });
                }

                var content = htmlInfo.Content;

                return JsonConvert.SerializeObject(new
                {
                    moduleId,
                    tabId = module.TabID,
                    content
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }
    }
}
