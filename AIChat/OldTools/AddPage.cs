using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AnthropicClient.Models;
using Dnn.PersonaBar.Library.Security;
using Dnn.PersonaBar.Pages.Components;
using Dnn.PersonaBar.Pages.Components.Exceptions;
using Dnn.PersonaBar.Pages.Components.Prompt.Models;
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

        public static string AddPage(string pageName, string pageTitle = "", string description = "", Int64 parentId = -1, bool visible = true)
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