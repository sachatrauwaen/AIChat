using System;
using System.Collections.Generic;
using System.Linq;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;
using Dnn.Mcp.WebApi;
using Dnn.PersonaBar.Pages.Components;
using Dnn.PersonaBar.Pages.Components.Exceptions;
using Dnn.PersonaBar.Pages.Components.Prompt.Models;

namespace Satrabel.AIChat.Tools
{
    public class UpdatePageTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "update-page",
                Title = "Update Page",
                Description = "Update an existing page in DNN portal",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "tabId",
                        Description = "The ID of the page to update",
                        Required = true,
                        Type = "number"
                    },
                    new ToolParameter
                    {
                        Name = "pageName",
                        Description = "The new name of the page",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "pageTitle",
                        Description = "The new title of the page",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "description",
                        Description = "The new description of the page",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "parentId",
                        Description = "The ID of the new parent page (-1 to keep current)",
                        Required = false,
                        Type = "number"
                    }
                },
                Handler = (arguments) =>
                {
                    var tabId = Convert.ToInt64(arguments["tabId"]);
                    var pageName = arguments.ContainsKey("pageName") ? arguments["pageName"].ToString() : "";
                    var pageTitle = arguments.ContainsKey("pageTitle") ? arguments["pageTitle"].ToString() : "";
                    var description = arguments.ContainsKey("description") ? arguments["description"].ToString() : "";
                    var parentId = arguments.ContainsKey("parentId") ? Convert.ToInt64(arguments["parentId"]) : -1;
                    
                    var result = UpdatePage(tabId, pageName, pageTitle, description, parentId);

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

        public string UpdatePage(Int64 tabId, string pageName = "", string pageTitle = "", string description = "", Int64 parentId = -1)
        {
            try
            {
                if (tabId <= 0)
                {
                    return "Error: Invalid page ID";
                }
                // Get the portal ID
                var portalId = PortalSettings.Current.PortalId;
                var portalSettings = new PortalSettings(portalId);
                var portalAliases = PortalAliasController.Instance.GetPortalAliasesByPortalId(portalId);
                portalSettings.PrimaryAlias = portalAliases.FirstOrDefault(a => a.IsPrimary);
                portalSettings.PortalAlias = PortalAliasController.Instance.GetPortalAlias(portalSettings.DefaultPortalAlias);
                var pagesController = PagesController.Instance;
                var pageUrl = "";
                var keywords = "";
                var pageSettings = pagesController.GetPageSettings((int)tabId, portalSettings);
                if (pageSettings == null)
                {
                    return "PageNotFound";
                }
                pageSettings.Name = !string.IsNullOrEmpty(pageName) ? pageName : pageSettings.Name;
                pageSettings.Title = !string.IsNullOrEmpty(pageTitle) ? pageTitle : pageSettings.Title;
                pageSettings.Url = !string.IsNullOrEmpty(pageUrl) ? pageUrl : pageSettings.Url;
                pageSettings.Description = !string.IsNullOrEmpty(description) ? description : pageSettings.Description;
                pageSettings.Keywords = !string.IsNullOrEmpty(keywords) ? keywords : pageSettings.Keywords;
                pageSettings.ParentId = parentId>-1 ? (int)parentId : pageSettings.ParentId;
                // pageSettings.IncludeInMenu = visible; //?? pageSettings.IncludeInMenu;
                if (!Dnn.PersonaBar.Pages.Components.Security.SecurityService.Instance.CanSavePageDetails(pageSettings))
                {
                    return "MethodPermissionDenied";
                }
                var updatedTab = pagesController.SavePageDetails(portalSettings, pageSettings);
                var lstResults = new List<PageModel> { new PageModel(updatedTab) };
                return "PageUpdatedMessage";
            }
            catch (PageNotFoundException)
            {
                return "PageNotFound";
            }
            catch (PageValidationException ex)
            {
                return ex.Message;
            }
        }
    }
} 
