
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using System.Reflection;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using Newtonsoft.Json;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class GetPagesTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-pages",
                Title = "Get Pages",
                Description = "Get list of pages of website",
                Category = "Pages",
                ReadOnly = true,
                Parameters = new List<ToolParameter> { },
                Handler = (arguments) =>
                {
                    //var ps = (PortalSettings)arguments["PortalSettings"];
                    var pages = GetPages(PortalSettings.Current);
                    var json = JsonConvert.SerializeObject(pages, Formatting.None);

                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock
                            {
                                Text = json
                            }
                        }
                    };
                }
            });
        }

        public object GetPages(PortalSettings ps)
        {
            var tc = new TabController();
            var tabs = tc.GetTabsByPortal(ps.PortalId)
                .Where(t=> t.Value.IsDeleted == false);
            return tabs.Select(t=> new { 
                t.Value.TabID, 
                t.Value.TabName, 
                t.Value.Title, 
                t.Value.Description,
                Url = t.Value.FullUrl,
                t.Value.ParentId,
            }).ToList();
        }
    }
}
