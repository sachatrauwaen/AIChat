using System;
using System.Collections.Generic;
using System.IO;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.FileSystem;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class ReadFileTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "read-file",
                Title = "Read File",
                Description = "Read content of a file from a specified path",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "filePath",
                        Description = "The path of the file to read",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var filePath = arguments["filePath"].ToString();
                    var result = ReadFile(filePath);

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

        public string ReadFile(string filePath)
        {
            try
            {
                // Check if the file path is a DNN relative path or absolute path
                if (string.IsNullOrEmpty(filePath))
                {
                    return "Error: File path cannot be empty";
                }

                var fileManager = FileManager.Instance;
                var folderManager = FolderManager.Instance;
                var portalId = PortalSettings.Current.PortalId;

                // Extract folder path and file name
                string folderPath = Path.GetDirectoryName(filePath).Replace("\\", "/");

                folderPath = folderPath.Trim('/');

                string fileName = Path.GetFileName(filePath);

                // Check if folder exists
                IFolderInfo folder = folderManager.GetFolder(portalId, folderPath);
                if (folder == null)
                {
                    return $"Error: Folder '{folderPath}' not found";
                }

                // Get file info
                IFileInfo file = fileManager.GetFile(folder, fileName);
                if (file == null)
                {
                    return $"Error: File '{fileName}' not found in folder '{folderPath}'";
                }

                // Read file content
                using (var stream = fileManager.GetFileContent(file))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
            }
        }
    }
} 
