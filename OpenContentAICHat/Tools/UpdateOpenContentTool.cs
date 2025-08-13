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
    public class UpdateOpenContentTool : IAIChatTool
    {


        public string Name => "Update OpenContent module";

        public string Description => "Update content of OpenContent Module";

        public MethodInfo Function => typeof(UpdateOpenContentTool).GetMethod(nameof(UpdateContent));

        public bool ReadOnly => false;

        public string UpdateContent(Int64 tabId, Int64 moduleId, string json)
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
                JArray lst = null;
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        lst = JArray.Parse(json);
                    }
                    catch (Exception ex)
                    {

                        return "Error: Invalid JSON format : " + ex.Message;
                    }
                }
                var dataList = ds.GetAll(dsContext, null).Items;
                foreach (var item in dataList)
                {
                    ds.Delete(dsContext, item);
                }
                if (lst != null)
                {
                    foreach (JObject item in lst)
                    {
                        ds.Add(dsContext, item);
                    }
                }
                return "updeted succesfull";
            }
            else
            {
                var dsContext = OpenContentUtils.CreateDataContext(moduleConfig, PortalSettings.Current.UserInfo.UserID, true);
                var dsItem = ds.Get(dsContext, null);
                if (dsItem == null)
                {
                    return "Error: No item found to update";
                }
                JToken data;
                try
                {
                    data = JToken.Parse(json);
                }
                catch (Exception ex)
                {

                    return "Error: Invalid JSON format : " + ex.Message;
                }

                ds.Update(dsContext, dsItem, data);
                return "Updated succesfully";
            }

        }
    }
}
