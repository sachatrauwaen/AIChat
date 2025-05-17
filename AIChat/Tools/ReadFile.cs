using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AnthropicClient.Models;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    class ReadFileTool : ITool
    {
        public string Name => "Read File";

        public string Description => "Read content of a file from a specified path";

        public MethodInfo Function => typeof(ReadFileTool).GetMethod(nameof(ReadFile));

        public static string ReadFile(string filePath)
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