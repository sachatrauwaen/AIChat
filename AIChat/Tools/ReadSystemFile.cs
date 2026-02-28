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
    public class ReadSystemFileTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "read-system-file",
                Title = "Read System File",
                Description = "Read content of a portal system file",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "path",
                        Description = "The path of the system file to read",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    if (!arguments.ContainsKey("path"))
                        throw new ArgumentException("Path is required");
                    var path = arguments["path"]?.ToString();
                    var result = ReadSystemFile(path);

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

        public string ReadSystemFile(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Path cannot be empty");
                // Check if the file path is provided
                if (Path.GetDirectoryName(path).Contains("."))
                    throw new ArgumentException("Path cannot contain dots");

                var portalRoot = PortalSettings.Current.HomeSystemDirectory;

                path = HttpContext.Current.Server.MapPath("~/" + portalRoot + path.TrimStart('/'));

                // Check if file exists
                if (!File.Exists(path))
                {
                    return $"Error: File '{path}' not found";
                }

                // Determine file size
                var fileInfo = new FileInfo(path);
                var fileSizeInMB = fileInfo.Length / (1024.0 * 1024.0);

                // Prevent reading extremely large files
                if (fileSizeInMB > 10) // Limit to 10MB
                {
                    return $"Error: File size ({fileSizeInMB:F2} MB) exceeds the maximum allowed size (10 MB)";
                }

                // Detect if the file is binary
                bool isBinary = IsBinaryFile(path);
                if (isBinary)
                {
                    return $"Error: Cannot read binary file '{Path.GetFileName(path)}'";
                }

                // Read the file content
                return File.ReadAllText(path);
                
                // Return file info and content
                //var result = new
                //{
                //    FileName = Path.GetFileName(path),
                //    FilePath = filePath,
                //    Size = Math.Round(fileInfo.Length / 1024.0, 2), // Size in KB
                //    LastModified = fileInfo.LastWriteTime,
                //    Content = content
                //};

                //return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
            }
        }

        // Utility method to check if a file is binary
        private bool IsBinaryFile(string filePath)
        {
            // Common binary file extensions
            string[] binaryExtensions = { ".exe", ".dll", ".pdb", ".zip", ".rar", ".7z", ".doc", ".docx", 
                                         ".xls", ".xlsx", ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp",
                                         ".mp3", ".mp4", ".avi", ".mov", ".wmv", ".wav" };

            // Check extension first
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (binaryExtensions.Contains(extension))
            {
                return true;
            }

            // For files without known binary extensions, check file content
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[Math.Min(4096, stream.Length)];
                stream.Read(buffer, 0, buffer.Length);

                // Check for null bytes in the first 4KB
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
} 