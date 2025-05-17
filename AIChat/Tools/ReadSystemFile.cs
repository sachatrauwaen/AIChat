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
    class ReadSystemFileTool : ITool
    {
        public string Name => "Read System File";

        public string Description => "Read content of a file using .NET file system APIs";

        public MethodInfo Function => typeof(ReadSystemFileTool).GetMethod(nameof(ReadSystemFile));

        public static string ReadSystemFile(string filePath)
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

                // Check if file exists
                if (!File.Exists(filePath))
                {
                    return $"Error: File '{filePath}' not found";
                }

                // Determine file size
                var fileInfo = new FileInfo(filePath);
                var fileSizeInMB = fileInfo.Length / (1024.0 * 1024.0);

                // Prevent reading extremely large files
                if (fileSizeInMB > 10) // Limit to 10MB
                {
                    return $"Error: File size ({fileSizeInMB:F2} MB) exceeds the maximum allowed size (10 MB)";
                }

                // Detect if the file is binary
                bool isBinary = IsBinaryFile(filePath);
                if (isBinary)
                {
                    return $"Error: Cannot read binary file '{Path.GetFileName(filePath)}'";
                }

                // Read the file content
                return File.ReadAllText(filePath);
                
                // Return file info and content
                //var result = new
                //{
                //    FileName = Path.GetFileName(filePath),
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
        private static bool IsBinaryFile(string filePath)
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