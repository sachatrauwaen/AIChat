
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

using ModulesControllerLibrary = Dnn.PersonaBar.Library.Controllers.ModulesController;
using DotNetNuke.Entities.Modules;
using System.Xml;
using DotNetNuke.Framework;
using DotNetNuke.Common;
using System.IO;
using DotNetNuke.Common.Utilities;
using System.Web.UI.WebControls;
using System.Net;

namespace Satrabel.AIChat.Tools
{
    class GetModulesTool : ITool
    {
        public string Name => "Get Modules";

        public string Description => "Get list of modules of page";

        public MethodInfo Function => typeof(GetModulesTool).GetMethod(nameof(GetModules));

        public static string GetModules(Int64 tabId)
        {
            var modules = ModulesControllerLibrary.Instance.GetModules(PortalSettings.Current,
                false, out int total, null, null, (int)tabId, 0, 1000);

            return JsonConvert.SerializeObject(modules.Select(t => new
            {
                t.ModuleID,
                // t.TabModuleID,
                t.DesktopModule.ModuleName,
                t.ModuleTitle,
                t.PaneName,
                // Data = GetData(t)
            }));
        }

        private static string GetData(ModuleInfo module)
        {
            // if (module.DesktopModule.ModuleName == "HTML")
            {
                //var htmlController = new DotNetNuke.Modules.Html.HtmlController();
                //var htmlModule = htmlController.GetHtml(module.ModuleID);
                if (!string.IsNullOrEmpty(module.DesktopModule.BusinessControllerClass) && module.DesktopModule.IsPortable)
                {
                    try
                    {
                        var businessControllerType = Reflection.CreateType(module.DesktopModule.BusinessControllerClass);

                        // Double-check
                        if (typeof(IPortable).IsAssignableFrom(businessControllerType))
                        {
                            // this.businessControllerProvider = Globals..GetRequiredService<IBusinessControllerProvider>();
                            XmlDocument moduleXml = new XmlDocument { XmlResolver = null };
                            XmlNode moduleNode = ModuleController.SerializeModule(moduleXml, module, true);
                            // var content =  moduleNode["content"].Value;


                            if (!string.IsNullOrEmpty(XmlUtils.GetNodeValue(moduleNode.CreateNavigator(), "content")))
                            {
                                string content = moduleNode.SelectSingleNode("content").InnerXml;
                                content = content.Substring(9, content.Length - 12);
                                var decodedContent = WebUtility.HtmlDecode(content);
                                return decodedContent;
                            }

                            //StringWriter sw = new StringWriter();
                            //XmlTextWriter xw = new XmlTextWriter(sw);
                            //moduleNode.WriteTo(xw);
                            //var content = sw.ToString();

                            //return content;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle exception
                        return string.Empty;
                    }

                }
            }
            return string.Empty;

        }
    }
}
