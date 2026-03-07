using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Prompt;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Satrabel.AIChat.Tools;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Datasource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Satrabel.OpenContentMcp.Tools
{
    public class ManageTemplatesTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {

            registry.RegisterTool(new ToolDefinition
            {
                Name = "init-opencontent-module",
                Title = "Init OpenContent Module",
                Description = "Init the OpenContent module",
                Category = "Open Content",
                Parameters = new List<ToolParameter>(){
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
                        Name = "templateName",
                        Description = "The name of the template",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {

                    var tabId = Convert.ToInt32(arguments["tabId"]);
                    var moduleId = Convert.ToInt32(arguments["moduleId"]);
                    var templateName = arguments["templateName"].ToString();
                    var result = InitOpenContentModule(tabId, moduleId, templateName);
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

            registry.RegisterTool(new ToolDefinition
            {
                Name = "add-opencontent-module",
                Title = "Add OpenContent Module",
                Description = "Add the OpenContent module",
                Category = "Open Content",
                Parameters = new List<ToolParameter>(){
                    new ToolParameter
                    {
                        Name = "tabId",
                        Description = "The ID of the page containing the module",
                        Required = true,
                        Type = "number"
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
                    },
                    new ToolParameter
                    {
                        Name = "templateName",
                        Description = "The name of the template",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {

                    var tabId = Convert.ToInt32(arguments["tabId"]);
                    var paneName = arguments.ContainsKey("paneName") ? arguments["paneName"].ToString() : "ContentPane";
                    var title = arguments.ContainsKey("title") ? arguments["title"].ToString() : "OpenContent Module";
                    var templateName = arguments["templateName"].ToString();
                    var mod = AddModuleTool.AddModule(tabId, "OpenContent", paneName, title);
                    var result = InitOpenContentModule(tabId, mod.ModuleID, templateName);
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

            // Register list-opencontent-templates tool
            registry.RegisterTool(new ToolDefinition
            {
                Name = "list-opencontent-templates",
                Title = "List OpenContent templates",
                Description = "List all available OpenContent templates in the portal",
                Category = "Open Content",
                Parameters = new List<ToolParameter>(),
                ReadOnly = true,
                Handler = (arguments) =>
                {
                    try
                    {
                        var templates = ListTemplates();

                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock
                                {
                                    Text = JsonConvert.SerializeObject(templates, Formatting.Indented)
                                }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        return new CallToolResult
                        {
                            IsError = true,
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock
                                {
                                    Text = $"Error listing templates: {ex.Message}"
                                }
                            }
                        };
                    }
                }
            });

            // Register get-opencontent-template tool
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-opencontent-template",
                Title = "Get OpenContent Template",
                Description = "Get details of a specific OpenContent Template",
                Category = "Open Content",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "name",
                        Description = "The name of the OpenContent template",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    try
                    {
                        var templatePath = arguments["name"].ToString();
                        var templateDetails = GetTemplate(templatePath);

                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock
                                {
                                    Text = JsonConvert.SerializeObject(templateDetails, Formatting.Indented)
                                }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        return new CallToolResult
                        {
                            IsError = true,
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock
                                {
                                    Text = $"Error getting template: {ex.Message}"
                                }
                            }
                        };
                    }
                }
            });

            // Register create-opencontent-template tool
            registry.RegisterTool(new ToolDefinition
            {
                Name = "save-opencontent-template",
                Title = "Save OpenContent Template",
                Description = "Save a new OpenContent template",
                Category = "Open Content",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "name",
                        Description = "The name of the template",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "template",
                        Description = "The content of the template (HTML/Handlebars)",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "schema",
                        Description = "The content of the schema",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "options",
                        Description = "The content of the options",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "data",
                        Description = "The content of the data",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "css",
                        Description = "The content of the CSS",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "js",
                        Description = "The content of the JS",
                        Required = false,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    try
                    {
                        var name = arguments["name"].ToString();
                        var template = arguments.ContainsKey("template") ? arguments["template"].ToString() : null;
                        var schema = arguments.ContainsKey("schema") ? arguments["schema"].ToString() : null;
                        var options = arguments.ContainsKey("options") ? arguments["options"].ToString() : null;
                        var data = arguments.ContainsKey("data") ? arguments["data"].ToString() : null;
                        var css = arguments.ContainsKey("css") ? arguments["css"].ToString() : null;
                        var js = arguments.ContainsKey("js") ? arguments["js"].ToString() : null;

                        var result = SaveTemplate(name, template, schema, options, data, css, js);

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
                    catch (Exception ex)
                    {
                        return new CallToolResult
                        {
                            IsError = true,
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock
                                {
                                    Text = $"Error saving template: {ex.Message}"
                                }
                            }
                        };
                    }
                }
            });

        }
        private List<string> ListTemplates()
        {
            var portalId = PortalSettings.Current.PortalId;
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var openContentPath = Path.Combine(portalPath, "OpenContent", "Templates");

            if (!Directory.Exists(openContentPath))
            {
                return new List<string>();
            }

            var templates = new List<string>();
            var templateDirs = Directory.GetDirectories(openContentPath);

            foreach (var dir in templateDirs)
            {
                var dirInfo = new DirectoryInfo(dir);
                var templateFiles = Directory.GetFiles(dir, "*.hbs").Concat(Directory.GetFiles(dir, "*.chtml")).ToList();

                templates.Add(dirInfo.Name);
            }

            return templates;
        }

        private TemplateInfo GetTemplate(string name)
        {
            var portalId = PortalSettings.Current.PortalId;
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var fullPath = Path.Combine(portalPath, "OpenContent", "Templates", name);

            // Check for associated schema and options files
            var dataPath = Path.Combine(fullPath, "data.json");
            var schemaPath = Path.Combine(fullPath, "schema.json");
            var optionsPath = Path.Combine(fullPath, "options.json");
            var templatePath = Path.Combine(fullPath, "template.hbs");
            var cssPath = Path.Combine(fullPath, "template.css");
            var jsPath = Path.Combine(fullPath, "template.js");

            string schemaContent = null;
            string optionsContent = null;
            string templateContent = null;
            string cssContent = null;
            string jsContent = null;

            string dataContent = null;
            if (File.Exists(dataPath))
            {
                dataContent = File.ReadAllText(dataPath);
            }
            if (File.Exists(schemaPath))
            {
                schemaContent = File.ReadAllText(schemaPath);
            }

            if (File.Exists(optionsPath))
            {
                optionsContent = File.ReadAllText(optionsPath);
            }
            if (File.Exists(templatePath))
            {
                templateContent = File.ReadAllText(templatePath);
            }
            if (File.Exists(cssPath))
            {
                cssContent = File.ReadAllText(cssPath);
            }
            if (File.Exists(jsPath))
            {
                jsContent = File.ReadAllText(jsPath);
            }

            return new TemplateInfo
            {
                Name = name,
                //FullPath = fullPath,
                Data = dataContent,
                Schema = schemaContent,
                Options = optionsContent,
                Template = templateContent,
                CSS = cssContent,
                JS = jsContent
            };
        }

        private string SaveTemplate(string name, string template, string schema, string options, string data, string css, string js)
        {
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var openContentPath = Path.Combine(portalPath, "OpenContent", "Templates", name);

            if (!Directory.Exists(openContentPath))
            {
                Directory.CreateDirectory(openContentPath);
            }


            var templatePath = Path.Combine(openContentPath, "template.hbs");
            var cssPath = Path.Combine(openContentPath, "template.css");
            var jsPath = Path.Combine(openContentPath, "template.js");
            var dataPath = Path.Combine(openContentPath, "data.json");
            var schemaPath = Path.Combine(openContentPath, "schema.json");
            var optionsPath = Path.Combine(openContentPath, "options.json");

            File.WriteAllText(templatePath, template);
            File.WriteAllText(cssPath, css);
            File.WriteAllText(jsPath, js);
            File.WriteAllText(dataPath, data);
            File.WriteAllText(schemaPath, schema);
            File.WriteAllText(optionsPath, options);
            return $"Template '{name}' saved successfully";
        }

        private string DeleteTemplate(string name)
        {
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var openContentPath = Path.Combine(portalPath, "OpenContent", "Templates", name);

            if (Directory.Exists(openContentPath))
            {
                Directory.Delete(openContentPath, true);
            }

            return $"Template '{name}' deleted successfully";
        }
        private string InitOpenContentModule(int tabId, int moduleId, string templateName)
        {
            try
            {
                var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
                var openContentPath = Path.Combine(portalPath, "OpenContent", "Templates", templateName);
                if (!Directory.Exists(openContentPath))
                {
                    return $"Template '{templateName}' not found";
                }
                var template = GetTemplate(templateName);
                FileUri templateUri = FileUri.FromPath(Path.Combine(openContentPath, "template.hbs"));
                var module = ModuleController.Instance.GetModule((int)moduleId, tabId, false);
                if (module == null)
                {
                    return $"Module not found.";
                }

                if (!string.Equals(module.DesktopModule?.ModuleName, "OpenContent", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Module is not an OpenContent module. Only OpenContent modules are supported.";
                }
                var data = template.Data;
                var templateSet = templateUri.FilePath;
                ModuleController.Instance.UpdateModuleSetting(moduleId, "template", templateSet);
                module.ModuleSettings["template"] = templateSet;
                //ModuleController.Instance.UpdateModule(module);
                //OpenContentSettings settings = module.OpenContentSettings();
                OpenContentModuleConfig moduleConfig = OpenContentModuleConfig.Create(module, PortalSettings.Current);
                IDataSource ds = DataSourceManager.GetDataSource(moduleConfig.Settings.Manifest.DataSource);

                if (moduleConfig.IsListMode())
                {
                    var dsContext = OpenContentUtils.CreateDataContext(moduleConfig, PortalSettings.Current.UserInfo.UserID, false);
                    var dsItems = ds.GetAll(dsContext, null);
                    ds.Add(dsContext, JObject.Parse(data));
                    return $"Template '{templateName}' initialized successfully";
                }
                else
                {
                    var dsContext = OpenContentUtils.CreateDataContext(moduleConfig, PortalSettings.Current.UserInfo.UserID, true);
                    var dsItem = ds.Get(dsContext, null);
                    if (dsItem == null)
                    {
                        ds.Add(dsContext, JObject.Parse(data));
                    }
                    else
                    {
                        ds.Update(dsContext, dsItem, JObject.Parse(data));
                    }
                    return $"Template '{templateName}' initialized successfully";
                }
            }
            catch (Exception ex)
            {
                return $"Error initializing template: {ex.Message}";
            }
        }
    }
}
