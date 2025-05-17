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
    class GetFilesTool : ITool
    {
        public string Name => "Get Files";

        public string Description => "Get list of files from a specific folder";

        public MethodInfo Function => typeof(GetFilesTool).GetMethod(nameof(GetFiles));

        public static string GetFiles(string folderPath = "")
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