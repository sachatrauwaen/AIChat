using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using System.Reflection;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class WriteSystemFileTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "write-system-file",
                Title = "Write System File",
                Description = "Write to a portal system file",
                Category = "System",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "filePath",
                        Description = "The path of the system file to write to",
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
                    if (!arguments.ContainsKey("filePath"))
                        throw new ArgumentException("File path is required");
                    var filePath = arguments["filePath"]?.ToString();
                    if (string.IsNullOrWhiteSpace(filePath))
                        throw new ArgumentException("File path cannot be empty");
                    if (!arguments.ContainsKey("content"))
                        throw new ArgumentException("Content is required");
                    var content = arguments["content"]?.ToString();
                    if (string.IsNullOrWhiteSpace(content))
                        throw new ArgumentException("Content cannot be empty");

                    var result = WriteSystemFile(filePath, content);

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

        public string WriteSystemFile(string filePath, string content)
        {
            try
            {
                // Check if the file path is provided
                if (string.IsNullOrEmpty(filePath))
                {
                    return "Error: File path cannot be empty";
                }
                if (Path.GetDirectoryName(filePath).Contains("."))
                {
                    return "Error: File path cannot contain dots";
                }

                var portalRoot = PortalSettings.Current.HomeSystemDirectory;

                filePath = HttpContext.Current.Server.MapPath("~/" + portalRoot + filePath.TrimStart('/'));

                // Ensure directory exists
                string directoryName = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryName))
                {
                    try
                    {
                        Directory.CreateDirectory(directoryName);
                    }
                    catch (Exception ex)
                    {
                        return $"Error creating directory: {ex.Message}";
                    }
                }

                // Security check: prevent writing to sensitive file types or locations
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                string[] restrictedExtensions = { ".exe", ".dll", ".config", ".asax", ".cs" };

                if (restrictedExtensions.Contains(extension))
                {
                    return $"Error: Writing to files with extension '{extension}' is not allowed for security reasons";
                }

                // Additional security checks
                string fileName = Path.GetFileName(filePath).ToLowerInvariant();
                if (fileName == "web.config")
                {
                    return "Error: Writing to web.config is not allowed for security reasons";
                }

                // Write the file content
                File.WriteAllText(filePath, content);

                // Get updated file info
                var fileInfo = new FileInfo(filePath);

                // Return success with file info
                var result = new
                {
                    Success = true,
                    Message = "File written successfully",
                    FileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    Size = Math.Round(fileInfo.Length / 1024.0, 2), // Size in KB
                    LastModified = fileInfo.LastWriteTime
                };

                return JsonConvert.SerializeObject(result);
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