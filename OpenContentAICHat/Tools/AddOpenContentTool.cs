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
    public class AddOpenContentTool : IAIChatTool
    {


        public string Name => "Add item to OpenContent module";

        public string Description => "Add item to OpenContent Module";

        public MethodInfo Function => typeof(AddOpenContentTool).GetMethod(nameof(AddContent));

        public bool ReadOnly => false;

        public string AddContent(Int64 tabId, Int64 moduleId, string json)
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
                // dsContext.Collection = collection;
                JToken data = null;
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        data = JObject.Parse(json);
                    }
                    catch (Exception ex)
                    {
                        return "Error: Invalid JSON format : " + ex.Message;
                    }
                }
                ds.Add(dsContext, data);
                return "updeted succesfull";
            }
            else
            {
                return "Error: not a multi items template";
            }
        }
    }
}
