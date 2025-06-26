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
using Dnn.PersonaBar.Pages.Services.Dto;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Security.Permissions;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    class DeletePageTool : ITool
    {
        public string Name => "Delete Page";

        public string Description => "Delete an existing page from DNN portal";

        public MethodInfo Function => typeof(DeletePageTool).GetMethod(nameof(DeletePage));

        public static string DeletePage(Int64 tabId)
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