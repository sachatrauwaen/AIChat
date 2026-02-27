using System;
using System.Collections.Generic;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using Dnn.Mcp.WebApi;
using Dnn.PersonaBar.Pages.Components;
using Dnn.PersonaBar.Pages.Components.Exceptions;
using Dnn.PersonaBar.Pages.Services.Dto;

namespace Satrabel.AIChat.Tools
{
    public class DeletePageTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "delete-page",
                Title = "Delete Page",
                Description = "Delete an existing page from DNN portal",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "tabId",
                        Description = "The ID of the page to delete",
                        Required = true,
                        Type = "number"
                    }
                },
                Handler = (arguments) =>
                {
                    var tabId = Convert.ToInt64(arguments["tabId"]);
                    var result = DeletePage(tabId);

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

        public string DeletePage(Int64 tabId)
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

                // Check if the page exists
                var tabController = new TabController();
                var existingTab = tabController.GetTab((int)tabId, portalId);
                if (existingTab == null)
                {
                    return $"Error: Page with ID '{tabId}' does not exist";
                }

                if (existingTab.IsDeleted)
                {
                    return $"Error: Page with ID '{tabId}' is already deleted";
                }

                var pagesController = PagesController.Instance;

                // Check permissions
                if (!Dnn.PersonaBar.Pages.Components.Security.SecurityService.Instance.CanDeletePage((int)tabId))
                {
                    return "MethodPermissionDenied";
                }

                // Delete the page
                pagesController.DeletePage(new PageItem { Id = (int)tabId }, portalSettings);

                return "PageDeleted";
            }
            catch (PageNotFoundException)
            {
                return "PageNotFound";
            }
            catch (PageValidationException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
} 
