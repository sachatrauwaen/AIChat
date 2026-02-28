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
    public class AddOpenContentTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "add-opencontent-item",
                Title = "Add OpenContent Item",
                Description = "Add item to OpenContent Module",
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
                    },
                    new ToolParameter
                    {
                        Name = "json",
                        Description = "The JSON data for the new item",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var tabId = Convert.ToInt64(arguments["tabId"]);
                    var moduleId = Convert.ToInt64(arguments["moduleId"]);
                    var json = arguments["json"].ToString();
                    
                    var result = AddContent(tabId, moduleId, json);

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

        public string AddContent(Int64 tabId, Int64 moduleId, string json)
        {
            try
            {
                //var module = ModulesControllerLibrary.Instance.GetModule(PortalSettings.Current,
                 //  (int)moduleId, (int)tabId, out KeyValuePair<HttpStatusCode, string> message);

                var module = ModuleController.Instance.GetModule((int)moduleId, (int)tabId, false);

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
                    JToken data = null;
                    
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            data = JObject.Parse(json);
                        }
                        catch (Exception ex)
                        {
                            return "Error: Invalid JSON format - " + ex.Message;
                        }
                    }
                    
                    ds.Add(dsContext, data);
                    return "Item added successfully";
                }
                else
                {
                    return "Error: Not a multi items template";
                }
            }
            catch (Exception ex)
            {
                return $"Error adding content: {ex.Message}";
            }
        }
    }
}
