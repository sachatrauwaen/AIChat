using System;
using System.Collections.Generic;
using System.Linq;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class GetFilesTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-files",
                Title = "Get Files",
                Description = "Get list of files from a specific folder",
                Category = "Files",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "folderPath",
                        Description = "The path of the folder to get files from (empty for root)",
                        Required = false,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var folderPath = arguments.ContainsKey("folderPath") ? arguments["folderPath"].ToString() : "";
                    var result = GetFiles(folderPath);

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

        public string GetFiles(string folderPath = "")
        {
            try
            {
                var fileManager = FileManager.Instance;
                var folderManager = FolderManager.Instance;
                var portalId = PortalSettings.Current.PortalId;
                folderPath = folderPath.Trim('/');
                // Handle empty folder path (root)
                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = "";
                }

                // Get folder info
                IFolderInfo folder = folderManager.GetFolder(portalId, folderPath);
                if (folder == null)
                {
                    return $"Error: Folder '{folderPath}' not found";
                }

                // Get files in the folder
                IEnumerable<IFileInfo> files = folderManager.GetFiles(folder);
                
                // Convert file info to anonymous objects with relevant properties
                var fileData = files.Select(f => new {
                    f.FileId,
                    f.FileName,
                    f.RelativePath,
                    f.Extension,
                    Size = f.Size / 1024.0, // Size in KB
                    // f.ContentType,
                    LastModified = f.LastModifiedOnDate,
                    // f.ContentItemID,
                    // f.Title,
                    // f.Description
                });

                return JsonConvert.SerializeObject(fileData);
            }
            catch (Exception ex)
            {
                return $"Error getting files: {ex.Message}";
            }
        }
    }
} 
