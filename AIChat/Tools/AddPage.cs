using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AnthropicClient.Models;
using Dnn.PersonaBar.Library.Security;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Security.Permissions;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    class AddPageTool : ITool
    {
        public string Name => "Add Page";

        public string Description => "Add a new page to DNN portal";

        public MethodInfo Function => typeof(AddPageTool).GetMethod(nameof(AddPage));

        public static string AddPage(string pageName, string pageTitle = "", string description = "", int parentId = -1, bool visible = true)
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

                // Check if the page already exists
                var tabController = new TabController();
                var existingTab = tabController.GetTabByName(pageName, portalId);
                if (existingTab != null && !existingTab.IsDeleted)
                {
                    return $"Error: A page with the name '{pageName}' already exists (TabID: {existingTab.TabID})";
                }

                // Validate parent page if specified
                if (parentId > 0)
                {
                    var parentTab = tabController.GetTab(parentId, portalId);
                    if (parentTab == null || parentTab.IsDeleted)
                    {
                        return $"Error: Parent page with ID {parentId} does not exist";
                    }
                }

                // Set up the new tab (page)
                var tab = new TabInfo
                {
                    PortalID = portalId,
                    TabName = pageName,
                    Title = !string.IsNullOrEmpty(pageTitle) ? pageTitle : pageName,
                    Description = description,
                    ParentId = parentId > 0 ? parentId : portalSettings.HomeTabId,
                    IsVisible = visible,
                    DisableLink = false,
                    IsDeleted = false,
                    //SkinSrc = portalSettings.DefaultPortalSkin,
                    //ContainerSrc = portalSettings.DefaultPortalContainer,
                };
                /*
                var pagesController = PagesController.Instance;

                if (pageSettings.ParentId != null)
                {
                    var parentTab = this.pagesController.GetPageSettings(pageSettings.ParentId.Value);
                    if (parentTab != null)
                    {
                        pageSettings.Permissions = parentTab.Permissions;
                    }
                }

                if (!SecurityService.Instance.CanSavePageDetails(pageSettings))
                {
                    return new ConsoleErrorResultModel(this.LocalizeString("MethodPermissionDenied"));
                }

                var newTab = this.pagesController.SavePageDetails(this.PortalSettings, pageSettings);
                */


                // Add the tab (page)
                tab.TabID = tabController.AddTab(tab);

                // Set permissions to match parent page or portal defaults
                if (parentId > 0)
                {
                    // Copy permissions from parent page
                    // TabPermissionController.CopyTabPermissions(parentId, tab.TabID);
                }
                else
                {
                    // Set default permissions
                    var portalController = new PortalController();
                    var portal = portalController.GetPortal(portalId);
                    if (portal != null)
                    {
                        // TabPermissionController.SaveTabPermissions(tab, portal.AdministratorRoleId);
                    }
                }

                // Get the created tab to return its details
                var createdTab = tabController.GetTab(tab.TabID, portalId);
                if (createdTab != null)
                {
                    // Return success with page details
                    var result = new
                    {
                        Success = true,
                        Message = "Page created successfully",
                        PageID = createdTab.TabID,
                        PageName = createdTab.TabName,
                        Title = createdTab.Title,
                        Url = createdTab.FullUrl,
                        ParentID = createdTab.ParentId,
                        IsVisible = createdTab.IsVisible
                    };

                    return JsonConvert.SerializeObject(result);
                }
                else
                {
                    return $"Error: Page was added with ID {tab.TabID} but could not be retrieved afterward";
                }
            }
            catch (Exception ex)
            {
                // Return error information
                var error = new
                {
                    Success = false,
                    Message = $"Error adding page: {ex.Message}"
                };

                return JsonConvert.SerializeObject(error);
            }
        }
    }
} 