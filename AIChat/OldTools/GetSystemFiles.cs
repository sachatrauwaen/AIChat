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
    class GetSystemFilesTool : ITool
    {
        public string Name => "Get System Files";

        public string Description => "Get list of files from a specific portal system folder";

        public MethodInfo Function => typeof(GetSystemFilesTool).GetMethod(nameof(GetSystemFiles));

        public static string GetSystemFiles(string folderPath = "")
        {
            try
            {
                var portalRoot = PortalSettings.Current.HomeSystemDirectory;
                // Handle empty folder path (root)
                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = HttpContext.Current.Server.MapPath("~/"+ portalRoot);
                }
                else
                {
                        folderPath = HttpContext.Current.Server.MapPath("~/"+ portalRoot + folderPath.Trim('/'));
                }

                // Verify that the folder exists
                if (!Directory.Exists(folderPath))
                {
                    return $"Error: Directory '{folderPath}' not found";
                }

                // Get files in the directory
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                FileInfo[] files = dirInfo.GetFiles();
                
                // Get sub-directories in the directory
                DirectoryInfo[] subDirs = dirInfo.GetDirectories();
                
                // Prepare response with both files and directories
                var result = new
                {
                    //CurrentPath = folderPath,
                    //RelativePath = GetRelativePath(folderPath),
                    Files = files.Select(f => new
                    {
                        FileName = f.Name,
                        RelativePath = folderPath+"/"+f.Name,
                        Extension = f.Extension,
                        Size = Math.Round(f.Length / 1024.0, 2), // Size in KB
                        LastModified = f.LastWriteTime,
                        //IsReadOnly = f.IsReadOnly,
                        //Attributes = f.Attributes.ToString()
                    }),
                    Directories = subDirs.Select(d => new
                    {
                        Name = d.Name,
                        FullPath = d.FullName,
                        RelativePath = GetRelativePath(d.FullName),
                        LastModified = d.LastWriteTime,
                        Attributes = d.Attributes.ToString()
                    })
                };

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return $"Error getting system files: {ex.Message}";
            }
        }

        private static string GetRelativePath(string fullPath)
        {
            try
            {
                string rootPath = HttpContext.Current.Server.MapPath("~/");
                
                // Ensure both paths use the same format
                rootPath = rootPath.TrimEnd('\\', '/') + "\\";
                fullPath = fullPath.Replace("/", "\\");
                
                if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    string relativePath = fullPath.Substring(rootPath.Length);
                    return "~/" + relativePath.Replace("\\", "/");
                }
                return fullPath;
            }
            catch
            {
                return fullPath;
            }
        }
    }
} 