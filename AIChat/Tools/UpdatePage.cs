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
    class UpdatePageTool : ITool
    {
        public string Name => "Update Page";

        public string Description => "Update an existing page in DNN portal";

        public MethodInfo Function => typeof(UpdatePageTool).GetMethod(nameof(UpdatePage));

        public static string UpdatePage(Int64 tabId, string pageName = "", string pageTitle = "", string description = "", Int64 parentId = -1, bool visible = true)
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
                pageSettings.IncludeInMenu = visible; //?? pageSettings.IncludeInMenu;
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