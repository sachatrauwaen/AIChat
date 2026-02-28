using System;
using System.Collections.Generic;
using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Datasource;
//using ModulesControllerLibrary = Dnn.PersonaBar.Library.Controllers.ModulesController;

namespace Satrabel.OpenContentMcp.Tools
{
    public class UpdateOpenContentTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "update-opencontent-items",
                Title = "Update OpenContent Items",
                Description = "Update content of OpenContent Module",
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
                        Description = "The JSON data to update (array for list mode, object for single mode)",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var tabId = Convert.ToInt64(arguments["tabId"]);
                    var moduleId = Convert.ToInt64(arguments["moduleId"]);
                    var json = arguments["json"].ToString();
                    
                    var result = UpdateContent(tabId, moduleId, json);

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

        public string UpdateContent(Int64 tabId, Int64 moduleId, string json)
        {
            try
            {
                //var module = ModulesControllerLibrary.Instance.GetModule(PortalSettings.Current,
                //   (int)moduleId, (int)tabId, out KeyValuePair<HttpStatusCode, string> message);

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
                    JArray lst = null;
                    
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            lst = JArray.Parse(json);
                        }
                        catch (Exception ex)
                        {
                            return "Error: Invalid JSON format - " + ex.Message;
                        }
                    }
                    
                    var dataList = ds.GetAll(dsContext, null).Items;
                    foreach (var item in dataList)
                    {
                        ds.Delete(dsContext, item);
                    }
                    
                    if (lst != null)
                    {
                        foreach (JObject item in lst)
                        {
                            ds.Add(dsContext, item);
                        }
                    }
                    
                    return "Items updated successfully";
                }
                else
                {
                    var dsContext = OpenContentUtils.CreateDataContext(moduleConfig, PortalSettings.Current.UserInfo.UserID, true);
                    var dsItem = ds.Get(dsContext, null);
                    
                    if (dsItem == null)
                    {
                        return "Error: No item found to update";
                    }
                    
                    JToken data;
                    try
                    {
                        data = JToken.Parse(json);
                    }
                    catch (Exception ex)
                    {
                        return "Error: Invalid JSON format - " + ex.Message;
                    }

                    ds.Update(dsContext, dsItem, data);
                    return "Item updated successfully";
                }
            }
            catch (Exception ex)
            {
                return $"Error updating content: {ex.Message}";
            }
        }
    }
}
