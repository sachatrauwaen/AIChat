
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
using DotNetNuke.Entities.Tabs;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    public class GetPagesTool : ITool, IAIChatTool
    {
        public string Name => "Get Pages";

        public string Description => "Get list of pages of website";

        public MethodInfo Function => typeof(GetPagesTool).GetMethod(nameof(GetPages));

        public bool ReadOnly => true;

        public string RulesFolder => string.Empty;

        public string GetPages()
        {
            var tc = new TabController();
            var tabs = tc.GetTabsByPortal(PortalSettings.Current.PortalId)
                .Where(t=> t.Value.IsDeleted == false);
            return JsonConvert.SerializeObject(tabs.Select(t=> new { 
                t.Value.TabID, 
                t.Value.TabName, 
                t.Value.Title, 
                t.Value.Description,
                Url = t.Value.FullUrl,
                t.Value.ParentId,
            }));
        }
    }
}
