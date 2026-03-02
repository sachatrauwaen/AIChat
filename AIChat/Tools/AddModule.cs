using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using System.Reflection;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Definitions;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.Localization;
using Newtonsoft.Json;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class AddModuleTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "add-module",
                Title = "Add Module",
                Description = "Add a module to a DNN page",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "tabId",
                        Description = "The ID of the page to add the module to",
                        Required = true,
                        Type = "number"
                    },
                    new ToolParameter
                    {
                        Name = "moduleName",
                        Description = "The name of the module to add",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "paneName",
                        Description = "The name of the pane to add the module to (default: ContentPane)",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "title",
                        Description = "The title of the module",
                        Required = false,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var tabId = Convert.ToInt64(arguments["tabId"]);
                    var moduleName = arguments["moduleName"].ToString();
                    var paneName = arguments.ContainsKey("paneName") ? arguments["paneName"].ToString() : "ContentPane";
                    var title = arguments.ContainsKey("title") ? arguments["title"].ToString() : "";
                    try
                    {
                        var result = AddModule(tabId, moduleName, paneName, title);
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock
                                {
                                    Text = $"Module added with ModuleDefID: {result.ModuleID}"
                                }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock
                                {
                                    Text = $"Error adding module: {ex.Message}"
                                }
                            }
                        };
                    }
                }
            });
        }

        public static ModuleInfo AddModule(Int64 tabId, string moduleName, string paneName = "ContentPane", string title = "")
        {

            if (tabId <= 0)
            {
                throw new Exception("Error: Invalid page ID");
            }

            if (string.IsNullOrEmpty(moduleName))
            {
                throw new Exception("Error: Module definition must be specified");
            }

            // Get the portal ID
            var portalId = PortalSettings.Current.PortalId;

            // Get the tab (page)
            var tabController = new TabController();
            var tab = tabController.GetTab((int)tabId, portalId);
            if (tab == null)
            {
                throw new Exception($"Error: Page with ID {tabId} not found");
            }

            // Find the module definition
            var moduleDefinitionController = new ModuleDefinitionController();
            var desktopModuleController = new DesktopModuleController();

            var desktopModule = DesktopModuleController.GetDesktopModuleByModuleName(moduleName, portalId);

            if (desktopModule == null)
            {
                throw new Exception($"Error: Module '{moduleName}' not found.");
            }

            // Validate pane name
            if (string.IsNullOrEmpty(paneName))
            {
                paneName = "ContentPane"; // Default pane
            }


            var message = default(KeyValuePair<HttpStatusCode, string>);

            if (!TabPermissionController.CanManagePage(tab))
            {
                throw new Exception("InsufficientPermissions");
            }

            var moduleList = new List<ModuleInfo>();

            var objModuleDefinition = ModuleDefinitionController.GetModuleDefinitionsByDesktopModuleID(desktopModule.DesktopModuleID).Values.FirstOrDefault();

            var objModule = new ModuleInfo();
            objModule.Initialize(portalId);

            objModule.PortalID = portalId;
            objModule.TabID = (int)tabId;
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
            return objModule;
        }
    }
}
