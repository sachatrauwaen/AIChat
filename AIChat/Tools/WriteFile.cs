using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using System.Reflection;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.FileSystem.Internal;
using Newtonsoft.Json;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class WriteFileTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "write-file",
                Title = "Write File",
                Description = "Write content to a file using DNN File System APIs",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "path",
                        Description = "The path of the file to write to",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "content",
                        Description = "The content to write to the file",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    if (!arguments.ContainsKey("path"))
                        throw new ArgumentException("Path is required");
                    if (!arguments.ContainsKey("content"))
                        throw new ArgumentException("Content is required");

                    var filePath = arguments["path"]?.ToString();
                    var content = arguments["content"]?.ToString();

                    var result = WriteFile(filePath, content);

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

        public string WriteFile(string filePath, string content)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty");
            if (content == null)
                throw new ArgumentException("Content cannot be null");

            try
            {
                var portalId = PortalSettings.Current.PortalId;
                var fileManager = FileManager.Instance;
                var folderManager = FolderManager.Instance;

                // Parse the file path into folder path and file name
                string folderPath = Path.GetDirectoryName(filePath).Replace("\\", "/");

                folderPath = folderPath.Trim('/');

                string fileName = Path.GetFileName(filePath);

                // Security check: prevent writing to sensitive file types
                string extension = Path.GetExtension(fileName).ToLowerInvariant();
                string[] restrictedExtensions = { ".exe", ".dll", ".config", ".asax", ".cs" };

                if (restrictedExtensions.Contains(extension))
                {
                    return $"Error: Writing to files with extension '{extension}' is not allowed for security reasons";
                }

                // Additional security checks
                if (fileName.ToLowerInvariant() == "web.config")
                {
                    return "Error: Writing to web.config is not allowed for security reasons";
                }

                // Get or create folder
                IFolderInfo folder = folderManager.GetFolder(portalId, folderPath);
                if (folder == null)
                {
                    try
                    {
                        folder = folderManager.AddFolder(portalId, folderPath);
                    }
                    catch (Exception ex)
                    {
                        return $"Error creating folder: {ex.Message}";
                    }
                }


                // Convert string content to byte array
                byte[] contentBytes = Encoding.UTF8.GetBytes(content);
                using (MemoryStream stream = new MemoryStream(contentBytes))
                {
                    // Check if file already exists
                    IFileInfo existingFile = fileManager.GetFile(folder, fileName);
                    if (existingFile != null)
                    {
                        var backupFolderPath = "backup/" + DateTime.Now.ToString("yyyyMMdd") + "/" + folderPath;
                        IFolderInfo backupFolder = folderManager.GetFolder(portalId, backupFolderPath);
                        if (backupFolder == null)
                        {
                            try
                            {
                                backupFolder = folderManager.AddFolder(portalId, backupFolderPath);
                            }
                            catch (Exception ex)
                            {
                                return $"Error creating backup folder: {ex.Message}";
                            }
                        }
                        // Update existing file
                        //fileManager.UpdateFile(existingFile, stream);
                        IFileInfo backupFile = null;
                        if (fileManager.GetFile(backupFolder, fileName) == null)
                        {
                            backupFile = fileManager.CopyFile(existingFile, backupFolder);
                        }
                        IFileInfo newFile = fileManager.AddFile(folder, fileName, stream, true);
                        // Return success with file info
                        var updateResult = new
                        {
                            Success = true,
                            Message = "File updated successfully",
                            FileName = existingFile.FileName,
                            RelativePath = existingFile.RelativePath,
                            Size = Math.Round(existingFile.Size / 1024.0, 2), // Size in KB
                            LastModified = existingFile.LastModifiedOnDate,
                            backup = backupFile == null ? "" : backupFolderPath
                        };

                        return JsonConvert.SerializeObject(updateResult);
                    }
                    else
                    {
                        // Add new file
                        IFileInfo newFile = fileManager.AddFile(folder, fileName, stream);

                        // Return success with file info
                        var addResult = new
                        {
                            Success = true,
                            Message = "File created successfully",
                            FileName = newFile.FileName,
                            FolderPath = newFile.RelativePath,
                            Size = Math.Round(newFile.Size / 1024.0, 2), // Size in KB
                            LastModified = newFile.LastModifiedOnDate
                        };

                        return JsonConvert.SerializeObject(addResult);
                    }
                }
            }
            catch (Exception ex)
            {
                // Return error information
                var error = new
                {
                    Success = false,
                    Message = $"Error writing file: {ex.Message}"
                };

                return JsonConvert.SerializeObject(error);
            }
        }
    }
}