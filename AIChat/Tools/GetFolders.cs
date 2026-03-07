using System.Collections.Generic;
using System.Linq;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class GetFoldersTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-folders",
                Title = "Get Folders",
                Description = "Get list of folders of website",
                Category = "Files",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "parentFolder",
                        Description = "The parent folder path (empty for all folders)",
                        Required = false,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var parentFolder = arguments.ContainsKey("parentFolder") ? arguments["parentFolder"].ToString() : "";
                    var result = GetFolders(parentFolder);

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

        public string GetFolders(string parentFolder = "")
        {
            var fc = FolderManager.Instance;
            parentFolder = parentFolder.Trim('/');
            IEnumerable<IFolderInfo> folders;
            if (string.IsNullOrEmpty(parentFolder) || (parentFolder == "/"))
            {
                folders = fc.GetFolders(PortalSettings.Current.PortalId);
            }
            else
            {
                var pf = fc.GetFolder(PortalSettings.Current.PortalId, parentFolder);
                folders = fc.GetFolders(pf);
            }
                
            return JsonConvert.SerializeObject(folders.Select(f => new
            {
                f.FolderID,
                f.FolderName,
                f.FolderPath,
                //f.DisplayName,
                //f.DisplayPath,
                //f.ParentID,
                //f.IsProtected,
                //f.IsVersioned,
                //f.StorageLocation
            }));
        }
    }
}
