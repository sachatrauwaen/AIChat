using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using AnthropicClient.Models;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    class WriteSystemFileTool : ITool
    {
        public string Name => "Write System File";

        public string Description => "Write content to a file using .NET file system APIs";

        public MethodInfo Function => typeof(WriteSystemFileTool).GetMethod(nameof(WriteSystemFile));

        public static string WriteSystemFile(string filePath, string content)
        {
            try
            {
                // Check if the file path is provided
                if (string.IsNullOrEmpty(filePath))
                {
                    return "Error: File path cannot be empty";
                }

                var portalRoot = PortalSettings.Current.HomeDirectory;

                // Handle portal-relative paths
                if (filePath.StartsWith("~/"))
                {
                    filePath = HttpContext.Current.Server.MapPath(filePath);
                }
                else
                {
                    // Assume path is relative to portal root
                    filePath = HttpContext.Current.Server.MapPath("~/" + portalRoot + filePath.TrimStart('/'));
                }

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