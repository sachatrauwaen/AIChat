using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Datasource;
using Satrabel.OpenContent.Components.Render;
//using ModulesControllerLibrary = Dnn.PersonaBar.Library.Controllers.ModulesController;

namespace Satrabel.OpenContentMcp.Tools
{
    public class GetOpenContentTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-opencontent-items",
                Title = "Get OpenContent Items",
                Description = "Get content of OpenContent Module",
                Category = "Open Content",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "tabId",
                        Description = "The ID of the page containing the module",
                        Required = true,
                        Type = "number"
                    },
                    new ToolParameter
                    {
                        Name = "moduleId",
                        Description = "The ID of the OpenContent module",
                        Required = true,
                        Type = "number"
                    }
                },
                Handler = (arguments) =>
                {
                    var tabId = Convert.ToInt64(arguments["tabId"]);
                    var moduleId = Convert.ToInt64(arguments["moduleId"]);

                    var result = GetContent(tabId, moduleId);

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

        public string GetContent(Int64 tabId, Int64 moduleId)
        {
            try
            {
                //var module = ModulesControllerLibrary.Instance.GetModule(PortalSettings.Current,
                //   (int)moduleId, (int)tabId, out KeyValuePair<HttpStatusCode, string> message);

                ModuleInfo module = ModuleController.Instance.GetModule((int)moduleId, (int)tabId, false);
                if (module == null)
                {
                    return "Error: Module not found";
                }

                if (module.DesktopModule.ModuleName != "OpenContent")
                {
                    return "Error: Only support OpenContent module";
                }

                OpenContentSettings settings = module.OpenContentSettings();
                OpenContentModuleConfig moduleConfig = OpenContentModuleConfig.Create(module, PortalSettings.Current);
                IDataSource ds = DataSourceManager.GetDataSource(moduleConfig.Settings.Manifest.DataSource);

                if (moduleConfig.IsListMode())
                {
                    var dsContext = OpenContentUtils.CreateDataContext(moduleConfig, PortalSettings.Current.UserInfo.UserID, false);
                    var dsItems = ds.GetAll(dsContext, null);

                    var res = new JObject();
                    var items = new JArray();
                    res["items"] = items;

                    foreach (var item in dsItems.Items)
                    {
                        items.Add(item.Data);
                    }

                    res["meta"] = new JObject();
                    res["meta"]["total"] = dsItems.Total;

                    return JsonConvert.SerializeObject(res, Formatting.Indented);
                }
                else
                {
                    var dsContext = OpenContentUtils.CreateDataContext(moduleConfig, PortalSettings.Current.UserInfo.UserID, true);
                    var dsItems = ds.Get(dsContext, null);
                    return JsonConvert.SerializeObject(dsItems.Data, Formatting.Indented);
                }
            }
            catch (Exception ex)
            {
                return $"Error getting content: {ex.Message}";
            }
        }
    }
}
