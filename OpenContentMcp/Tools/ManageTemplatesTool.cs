using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json;

namespace Satrabel.OpenContentMcp.Tools
{
    public class ManageTemplatesTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            // Register list-opencontent-templates tool
            registry.RegisterTool(new ToolDefinition
            {
                Name = "list-opencontent-folders",
                Title = "List OpenContent Folders",
                Description = "List all available OpenContent folders in the portal",
                Parameters = new List<ToolParameter>(),
                Handler = (arguments) =>
                {
                    try
                    {
                        var templates = ListFolders();
                        
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
                Name = "get-opencontent-folder",
                Title = "Get OpenContent Folder",
                Description = "Get details of a specific OpenContent Folder",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "name",
                        Description = "The name of the OpenContent folder",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    try
                    {
                        var templatePath = arguments["name"].ToString();
                        var templateDetails = GetFolder(templatePath);
                        
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
                Name = "create-opencontent-template",
                Title = "Create OpenContent Template",
                Description = "Create a new OpenContent template",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "templateName",
                        Description = "The name of the template",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "templateContent",
                        Description = "The content of the template (HTML/Handlebars)",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "schemaJson",
                        Description = "Optional JSON schema for the template",
                        Required = false,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "optionsJson",
                        Description = "Optional JSON options for the template",
                        Required = false,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    try
                    {
                        var templateName = arguments["templateName"].ToString();
                        var templateContent = arguments["templateContent"].ToString();
                        var schemaJson = arguments.ContainsKey("schemaJson") ? arguments["schemaJson"].ToString() : null;
                        var optionsJson = arguments.ContainsKey("optionsJson") ? arguments["optionsJson"].ToString() : null;
                        
                        var result = CreateTemplate(templateName, templateContent, schemaJson, optionsJson);
                        
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
                                    Text = $"Error creating template: {ex.Message}"
                                }
                            }
                        };
                    }
                }
            });

            // Register update-opencontent-template tool
            registry.RegisterTool(new ToolDefinition
            {
                Name = "update-opencontent-template",
                Title = "Update OpenContent Template",
                Description = "Update an existing OpenContent template",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "templatePath",
                        Description = "The path to the template file",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "templateContent",
                        Description = "The new content of the template",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    try
                    {
                        var templatePath = arguments["templatePath"].ToString();
                        var templateContent = arguments["templateContent"].ToString();
                        
                        var result = UpdateTemplate(templatePath, templateContent);
                        
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
                                    Text = $"Error updating template: {ex.Message}"
                                }
                            }
                        };
                    }
                }
            });

            // Register delete-opencontent-template tool
            registry.RegisterTool(new ToolDefinition
            {
                Name = "delete-opencontent-template",
                Title = "Delete OpenContent Template",
                Description = "Delete an OpenContent template",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "templatePath",
                        Description = "The path to the template file",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    try
                    {
                        var templatePath = arguments["templatePath"].ToString();
                        var result = DeleteTemplate(templatePath);
                        
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
                                    Text = $"Error deleting template: {ex.Message}"
                                }
                            }
                        };
                    }
                }
            });
        }

        private List<object> ListFolders()
        {
            var portalId = PortalSettings.Current.PortalId;
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var openContentPath = Path.Combine(portalPath, "OpenContent", "Templates");

            if (!Directory.Exists(openContentPath))
            {
                return new List<object>();
            }

            var templates = new List<object>();
            var templateDirs = Directory.GetDirectories(openContentPath);

            foreach (var dir in templateDirs)
            {
                var dirInfo = new DirectoryInfo(dir);
                var templateFiles = Directory.GetFiles(dir, "*.hbs").Concat(Directory.GetFiles(dir, "*.html")).ToList();

                foreach (var file in templateFiles)
                {
                    var fileInfo = new FileInfo(file);
                    templates.Add(new
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file.Replace(portalPath, "~"),
                        FullPath = file,
                        Category = dirInfo.Name,
                        Extension = fileInfo.Extension,
                        Size = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    });
                }
            }

            return templates;
        }

        private object GetFolder(string name)
        {
            var portalId = PortalSettings.Current.PortalId;
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var fullPath = Path.Combine(portalPath, "OpenContent", "Templates", name);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Template not found: {name}");
            }

            var fileInfo = new FileInfo(fullPath);
            var content = File.ReadAllText(fullPath);

            // Check for associated schema and options files
            var dataPath = Path.Combine(fullPath, "schema.json");
            var schemaPath = Path.Combine(fullPath, "schema.json");
            var optionsPath = Path.Combine(fullPath, "options.json");

            string schemaContent = null;
            string optionsContent = null;

            if (File.Exists(schemaPath))
            {
                schemaContent = File.ReadAllText(schemaPath);
            }

            if (File.Exists(optionsPath))
            {
                optionsContent = File.ReadAllText(optionsPath);
            }

            return new
            {
                Name = name,
                FullPath = fullPath,
                Data = content,
                Schema = schemaContent,
                Options = optionsContent,
            };
        }

        private string CreateTemplate(string templateName, string templateContent, string schemaJson, string optionsJson)
        {
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var openContentPath = Path.Combine(portalPath, "OpenContent", "Templates", "Custom");

            if (!Directory.Exists(openContentPath))
            {
                Directory.CreateDirectory(openContentPath);
            }

            var templateFileName = $"{templateName}.hbs";
            var templatePath = Path.Combine(openContentPath, templateFileName);

            if (File.Exists(templatePath))
            {
                return $"Error: Template '{templateName}' already exists";
            }

            // Write template file
            File.WriteAllText(templatePath, templateContent);

            // Write schema file if provided
            if (!string.IsNullOrEmpty(schemaJson))
            {
                var schemaPath = Path.Combine(openContentPath, $"{templateName}.schema.json");
                File.WriteAllText(schemaPath, schemaJson);
            }

            // Write options file if provided
            if (!string.IsNullOrEmpty(optionsJson))
            {
                var optionsPath = Path.Combine(openContentPath, $"{templateName}.options.json");
                File.WriteAllText(optionsPath, optionsJson);
            }

            return $"Template '{templateName}' created successfully at: {templatePath.Replace(portalPath, "~")}";
        }

        private string UpdateTemplate(string templatePath, string templateContent)
        {
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var fullPath = templatePath.StartsWith("~") 
                ? templatePath.Replace("~", portalPath) 
                : templatePath;

            if (!File.Exists(fullPath))
            {
                return $"Error: Template not found: {templatePath}";
            }

            File.WriteAllText(fullPath, templateContent);

            return $"Template updated successfully: {templatePath}";
        }

        private string DeleteTemplate(string templatePath)
        {
            var portalPath = PortalSettings.Current.HomeDirectoryMapPath;
            var fullPath = templatePath.StartsWith("~") 
                ? templatePath.Replace("~", portalPath) 
                : templatePath;

            if (!File.Exists(fullPath))
            {
                return $"Error: Template not found: {templatePath}";
            }

            // Delete the template file
            File.Delete(fullPath);

            // Delete associated schema and options files if they exist
            var directory = Path.GetDirectoryName(fullPath);
            var baseName = Path.GetFileNameWithoutExtension(fullPath);
            var schemaPath = Path.Combine(directory, $"{baseName}.schema.json");
            var optionsPath = Path.Combine(directory, $"{baseName}.options.json");

            if (File.Exists(schemaPath))
            {
                File.Delete(schemaPath);
            }

            if (File.Exists(optionsPath))
            {
                File.Delete(optionsPath);
            }

            return $"Template deleted successfully: {templatePath}";
        }
    }
}
