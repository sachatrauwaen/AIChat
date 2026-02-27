using System;
using System.Collections.Generic;
using System.Linq;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using Dnn.Mcp.WebApi;
using Dnn.PersonaBar.Pages.Components;
using Dnn.PersonaBar.Pages.Components.Exceptions;
using Dnn.PersonaBar.Pages.Components.Prompt.Models;

namespace Satrabel.AIChat.Tools
{
    public class AddPageTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "add-page",
                Title = "Add Page",
                Description = "Add a new page to DNN portal",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "pageName",
                        Description = "The name of the page to create",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "pageTitle",
                        Description = "The title of the page",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "description",
                        Description = "The description of the page",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "parentId",
                        Description = "The ID of the parent page (-1 for root level)",
                        Required = false,
                        Type = "number"
                    },
                    new ToolParameter
                    {
                        Name = "visible",
                        Description = "Whether the page should be visible in the menu",
                        Required = false,
                        Type = "boolean"
                    }
                },
                Handler = (arguments) =>
                {
                    var pageName = arguments["pageName"].ToString();
                    var pageTitle = arguments.ContainsKey("pageTitle") ? arguments["pageTitle"].ToString() : "";
                    var description = arguments.ContainsKey("description") ? arguments["description"].ToString() : "";
                    var parentId = arguments.ContainsKey("parentId") ? Convert.ToInt64(arguments["parentId"]) : -1;
                    var visible = arguments.ContainsKey("visible") ? Convert.ToBoolean(arguments["visible"]) : true;
                    
                    var result = AddPage(pageName, pageTitle, description, parentId, visible);

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

        public string AddPage(string pageName, string pageTitle = "", string description = "", Int64 parentId = -1, bool visible = true)
        {
            try
            {
                if (string.IsNullOrEmpty(pageName))
                {
                    return "Error: Page name cannot be empty";
                }

                // Get the portal ID
                var portalId = PortalSettings.Current.PortalId;
                var portalSettings = new PortalSettings(portalId);

                var portalAliases = PortalAliasController.Instance.GetPortalAliasesByPortalId(portalId);
                portalSettings.PrimaryAlias = portalAliases.FirstOrDefault(a => a.IsPrimary);
                portalSettings.PortalAlias = PortalAliasController.Instance.GetPortalAlias(portalSettings.DefaultPortalAlias);


                // Check if the page already exists
                var tabController = new TabController();
                var existingTab = tabController.GetTabByName(pageName, portalId);
                if (existingTab != null && !existingTab.IsDeleted)
                {
                    return $"Error: A page with the name '{pageName}' already exists (TabID: {existingTab.TabID})";
                }

                // Validate parent page if specified
                if (parentId > -1)
                {
                    var parentTab = tabController.GetTab((int)parentId, portalId);
                    if (parentTab == null || parentTab.IsDeleted)
                    {
                        return $"Error: Parent page with ID {parentId} does not exist";
                    }
                }
                var pageUrl = "";
                var keywords = "";

                var pagesController = PagesController.Instance;


                var pageSettings = pagesController.GetDefaultSettings();
                pageSettings.Name = !string.IsNullOrEmpty(pageName) ? pageName : pageSettings.Name;
                pageSettings.Title = !string.IsNullOrEmpty(pageTitle) ? pageTitle : pageSettings.Title;
                pageSettings.Url = !string.IsNullOrEmpty(pageUrl) ? pageUrl : pageSettings.Url;
                pageSettings.Description = !string.IsNullOrEmpty(description) ? description : pageSettings.Description;
                pageSettings.Keywords = !string.IsNullOrEmpty(keywords) ? keywords : pageSettings.Keywords;
                pageSettings.ParentId = parentId > -1 ? (int)parentId : pageSettings.ParentId;
                pageSettings.HasParent = parentId > -1;
                pageSettings.IncludeInMenu = visible ? visible : pageSettings.IncludeInMenu;
                if (pageSettings.ParentId != null)
                {
                    var parentTab = pagesController.GetPageSettings(pageSettings.ParentId.Value);
                    if (parentTab != null)
                    {
                        pageSettings.Permissions = parentTab.Permissions;
                    }
                }

                if (!Dnn.PersonaBar.Pages.Components.Security.SecurityService.Instance.CanSavePageDetails(pageSettings))
                {
                    return "MethodPermissionDenied";
                }

                var newTab = pagesController.SavePageDetails(portalSettings, pageSettings);

                // create the tab
                var lstResults = new List<PageModel>();
                if (newTab != null && newTab.TabID > 0)
                {
                    return newTab.TabID.ToString();
                }
                else
                {

                    return "PageCreateFailed";
                }

                // return "PageCreated";
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
