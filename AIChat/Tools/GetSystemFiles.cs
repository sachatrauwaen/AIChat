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
    public class GetSystemFilesTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-system-files",
                Title = "Get System Files",
                Description = "Get list of files from a specific portal system folder",
                Category = "System",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "folderPath",
                        Description = "The path of the system folder to get files from (empty for root)",
                        Required = false,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var folderPath = arguments.ContainsKey("folderPath") ? arguments["folderPath"].ToString() : "";
                    var result = GetSystemFiles(folderPath);

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

        public string GetSystemFiles(string folderPath = "")
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

        private string GetRelativePath(string fullPath)
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