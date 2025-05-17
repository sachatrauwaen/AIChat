using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AnthropicClient.Models;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Definitions;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.Localization;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    class AddModuleTool : ITool
    {
        public string Name => "Add Module";

        public string Description => "Add a module to a DNN page";

        public MethodInfo Function => typeof(AddModuleTool).GetMethod(nameof(AddModule));

        public static string AddModule(int tabId, string moduleName, string paneName = "ContentPane", string title = "")
        {
            try
            {
                if (tabId <= 0)
                {
                    return "Error: Invalid page ID";
                }

                if (string.IsNullOrEmpty(moduleName))
                {
                    return "Error: Module definition must be specified";
                }

                // Get the portal ID
                var portalId = PortalSettings.Current.PortalId;

                // Get the tab (page)
                var tabController = new TabController();
                var tab = tabController.GetTab(tabId, portalId);
                if (tab == null)
                {
                    return $"Error: Page with ID {tabId} not found";
                }

                // Find the module definition
                var moduleDefinitionController = new ModuleDefinitionController();
                var desktopModuleController = new DesktopModuleController();

                var desktopModule = DesktopModuleController.GetDesktopModuleByModuleName(moduleName, portalId);

                if (desktopModule == null)
                {
                    return $"Error: Module '{moduleName}' not found.";
                }

                // Validate pane name
                if (string.IsNullOrEmpty(paneName))
                {
                    paneName = "ContentPane"; // Default pane
                }


                var message = default(KeyValuePair<HttpStatusCode, string>);
               
                if (!TabPermissionController.CanManagePage(tab))
                {
                    return "InsufficientPermissions";
                }

                var moduleList = new List<ModuleInfo>();

                foreach (var objModuleDefinition in ModuleDefinitionController.GetModuleDefinitionsByDesktopModuleID(desktopModule.DesktopModuleID).Values)
                {
                    var objModule = new ModuleInfo();
                    objModule.Initialize(portalId);

                    objModule.PortalID = portalId;
                    objModule.TabID = tabId;
                    objModule.ModuleOrder = 0;
                    objModule.ModuleTitle = string.IsNullOrEmpty(title) ? objModuleDefinition.FriendlyName : title;
                    objModule.PaneName = paneName;
                    objModule.ModuleDefID = objModuleDefinition.ModuleDefID;
                    if (objModuleDefinition.DefaultCacheTime > 0)
                    {
                        objModule.CacheTime = objModuleDefinition.DefaultCacheTime;
                        if (PortalSettings.Current.DefaultModuleId > Null.NullInteger &&
                            PortalSettings.Current.DefaultTabId > Null.NullInteger)
                        {
                            var defaultModule = ModuleController.Instance.GetModule(
                                PortalSettings.Current.DefaultModuleId,
                                PortalSettings.Current.DefaultTabId,
                                true);
                            if (defaultModule != null)
                            {
                                objModule.CacheTime = defaultModule.CacheTime;
                            }
                        }
                    }

                    ModuleController.Instance.InitialModulePermission(objModule, objModule.TabID, 0);

                    if (PortalSettings.Current.ContentLocalizationEnabled)
                    {
                        var defaultLocale = LocaleController.Instance.GetDefaultLocale(PortalSettings.Current.PortalId);

                        // check whether original tab is exists, if true then set culture code to default language,
                        // otherwise set culture code to current.
                        objModule.CultureCode =
                            TabController.Instance.GetTabByCulture(objModule.TabID, PortalSettings.Current.PortalId, defaultLocale) !=
                            null
                                ? defaultLocale.Code
                                : PortalSettings.Current.CultureCode;
                    }
                    else
                    {
                        objModule.CultureCode = Null.NullString;
                    }

                    objModule.AllTabs = false;
                    objModule.Alignment = null;

                    ModuleController.Instance.AddModule(objModule);
                    moduleList.Add(objModule);

                    // Set position so future additions to page can operate correctly
                    var position = ModuleController.Instance.GetTabModule(objModule.TabModuleID).ModuleOrder + 1;
                }

                if (moduleList == null)
                {
                    return message.Value;
                }

                if (moduleList.Count == 0)
                {
                    return "Prompt_NoModulesAdded";
                }

                var modules = moduleList.Select(newModule => ModuleController.Instance.GetTabModule(newModule.TabModuleID)).ToList();

                return $"ModuleAdded  {modules.Count}, {(moduleList.Count == 1 ? string.Empty : "s")}";


            }
            catch (Exception ex)
            {
                // Return error information
                var error = new
                {
                    Success = false,
                    Message = $"Error adding module: {ex.Message}"
                };

                return JsonConvert.SerializeObject(error);
            }
        }
    }
}