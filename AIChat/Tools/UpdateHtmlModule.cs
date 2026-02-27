using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content.Workflow;
using DotNetNuke.Entities.Content.Workflow.Entities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Modules.Html;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Satrabel.AIChat.Tools
{
    /// <summary>
    /// MCP tool to update HTML content of a DNN HTML module.
    /// </summary>
    public class UpdateHtmlModuleTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "update-html",
                Title = "Update HTML Module Content",
                Description = "Update the HTML content of a DNN HTML module by module ID.",
                Parameters = new List<ToolParameter>
                {
                     new ToolParameter
                    {
                        Name = "tabId",
                        Description = "The ID of the page containing the module (for validation).",
                        Required = true,
                        Type = "number"
                    },
                    new ToolParameter
                    {
                        Name = "moduleId",
                        Description = "The ID of the HTML module to update",
                        Required = true,
                        Type = "number"
                    },
                    new ToolParameter
                    {
                        Name = "content",
                        Description = "The new HTML content to set",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var moduleId = Convert.ToInt32(arguments["moduleId"]);
                    var content = arguments["content"]?.ToString() ?? string.Empty;
                    int tabId = Convert.ToInt32(arguments["tabId"]);

                    var result = UpdateHtml(moduleId, content, tabId);

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
        /// Updates the HTML content of a DNN HTML module using reflection (DotNetNuke.Modules.Html).
        /// </summary>
        public string UpdateHtml(int moduleId, string content, int tabId)
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

               
                var htmlTextController = new DotNetNuke.Modules.Html.HtmlTextController();
                var workflowID = htmlTextController.GetWorkflow(moduleId, tabId, PortalSettings.Current.PortalId).Value;
                var htmlInfo = htmlTextController.GetTopHtmlText(moduleId, false, workflowID);
                if (htmlInfo == null)
                {
                    return JsonConvert.SerializeObject(new { error = "HTML content not found." });
                }
                var workflowManager = WorkflowManager.Instance;
                var workflow = workflowManager.GetWorkflow(workflowID);
                var workflowStates = workflow.States.ToList();
                var maxVersions = htmlTextController.GetMaximumVersionHistory(PortalSettings.Current.PortalId);

                htmlInfo.Content = content;
                htmlTextController.UpdateHtmlText(htmlInfo, maxVersions);

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    moduleId,
                    message = "HTML content updated successfully."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }
    }
}
