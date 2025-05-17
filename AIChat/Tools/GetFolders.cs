using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AnthropicClient.Models;
using System.Reflection;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    class GetFoldersTool : ITool
    {
        public string Name => "Get Folders";

        public string Description => "Get list of folders of website";

        public MethodInfo Function => typeof(GetFoldersTool).GetMethod(nameof(GetFolders));

        public static string GetFolders(string parentFolder = "")
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