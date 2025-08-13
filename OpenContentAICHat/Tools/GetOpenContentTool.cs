using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Satrabel.AIChat.Services;
using Satrabel.AIChat.Tools;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Datasource;
using Satrabel.OpenContent.Components.Render;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using ModulesControllerLibrary = Dnn.PersonaBar.Library.Controllers.ModulesController;

namespace OpenContentAICHat.Tools
{
    public class GetOpenContentTool : IAIChatTool
    {


        public string Name => "Get OpenContent module";

        public string Description => "Get content of OpenContent Module";

        public MethodInfo Function => typeof(GetOpenContentTool).GetMethod(nameof(GeContent));

        public bool ReadOnly => true;

        public string GeContent(Int64 tabId, Int64 moduleId/*, string collection = "Items"*/)
        {
            var module = ModulesControllerLibrary.Instance.GetModule(PortalSettings.Current,
               (int)moduleId, (int)tabId, out KeyValuePair<HttpStatusCode, string> message);

            if (module == null) return string.Empty;

            if (module.DesktopModule.ModuleName != "OpenContent") return "Error : Only support OpenContent module";

            OpenContentSettings settings = module.OpenContentSettings();
            OpenContentModuleConfig moduleConfig = OpenContentModuleConfig.Create(module, PortalSettings.Current);
            IDataSource ds = DataSourceManager.GetDataSource(moduleConfig.Settings.Manifest.DataSource);
            if (moduleConfig.IsListMode())
            {

                
                var dsContext = OpenContentUtils.CreateDataContext(moduleConfig, PortalSettings.Current.UserInfo.UserID, false);
                //dsContext.Collection = collection;
                var dsItems = ds.GetAll(dsContext, null);
                // var mf = new ModelFactoryMultiple(dsItems.Items, moduleConfig, collection);
                // var model = mf.GetModelAsJson(false);
                var res = new JObject();
                var items = new JArray();
                res["items"] = items;
                foreach (var item in dsItems.Items)
                {
                    // item["id"] = item["Context"]["Id"];
                    // JsonUtils.IdJson(item);
                    items.Add(item.Data);
                }
                res["meta"]["total"] = dsItems.Total;
                return JsonConvert.SerializeObject(res, Formatting.Indented);
            }
            else
            {
                var dsContext = OpenContentUtils.CreateDataContext(moduleConfig, PortalSettings.Current.UserInfo.UserID, true);
                var dsItems = ds.Get(dsContext, null);
                return JsonConvert.SerializeObject(dsItems.Data, Formatting.Indented);
            }

        }
    }
}
