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
using Dnn.PersonaBar.Pages.Components.Dto;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Security.Permissions;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    class GetPageTool : ITool
    {
        public string Name => "Get Page";

        public string Description => "Get details of an existing page from DNN portal";

        public MethodInfo Function => typeof(GetPageTool).GetMethod(nameof(GetPage));

        public static string GetPage(int pageId)
        {
            try
            {
                if (pageId <= 0)
                {
                    return "Error: Invalid page ID";
                }

                // Get the portal ID
                var portalId = PortalSettings.Current.PortalId;

                var pagesController = PagesController.Instance;

                // Get page settings
                var pageSettings = pagesController.GetPageSettings(pageId);
                if (pageSettings == null)
                {
                    return $"Error: Could not retrieve settings for page with ID {pageId}";
                }

                // Convert to JSON and return
                var pageJson = JsonConvert.SerializeObject(pageSettings, Formatting.Indented);
                return pageJson;
            }
            catch (PageNotFoundException)
            {
                return "PageNotFound";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static string GetPageByName(string pageName)
        {
            try
            {
                if (string.IsNullOrEmpty(pageName))
                {
                    return "Error: Page name cannot be empty";
                }

                // Get the portal ID
                var portalId = PortalSettings.Current.PortalId;

                // Check if the page exists
                var tabController = new TabController();
                var existingTab = tabController.GetTabByName(pageName, portalId);
                if (existingTab == null || existingTab.IsDeleted)
                {
                    return $"Error: Page with name '{pageName}' does not exist or is deleted";
                }

                var pagesController = PagesController.Instance;

                // Get page settings
                var pageSettings = pagesController.GetPageSettings(existingTab.TabID);
                if (pageSettings == null)
                {
                    return $"Error: Could not retrieve settings for page with name {pageName}";
                }

                // Convert to JSON and return
                var pageJson = JsonConvert.SerializeObject(pageSettings, Formatting.Indented);
                return pageJson;
            }
            catch (PageNotFoundException)
            {
                return "PageNotFound";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
} 