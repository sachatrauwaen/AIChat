
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
    class GetHTMLModuleTool : ITool
    {
        public string Name => "Get HTML Module content";

        public string Description => "Get content of DNN_HTML module";

        public MethodInfo Function => typeof(GetHTMLModuleTool).GetMethod(nameof(GetModule));

        public static string GetModule(Int64 tabId, Int64 moduleId)
        {
            var module = ModulesControllerLibrary.Instance.GetModule(PortalSettings.Current,
                (int)moduleId, (int)tabId, out KeyValuePair<HttpStatusCode, string> message);

            if (module == null) return string.Empty;

            return GetData(module);
        }

        private static string GetData(ModuleInfo module)
        {
            var dataPath= string.Empty;
            if (module.DesktopModule.ModuleName == "DNN_HTML")
            {
                dataPath = "/htmltext/content";
            }
            else
            {
                return "Error : Only support html module";
            }

            //else if (module.DesktopModule.ModuleName == "OpenContent")
            //{
            //    dataPath = "/opencontent/item/json";
            //}

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

                            object obj = Reflection.CreateObject(module.DesktopModule.BusinessControllerClass, module.DesktopModule.BusinessControllerClass);
                            if (!(obj is IPortable portable))
                            {
                                return string.Empty;
                            }

                            string text = Convert.ToString(portable.ExportModule(module.ModuleID));
                            if (string.IsNullOrEmpty(text))
                            {
                                return string.Empty;
                            }

                            XmlDocument contentXml = new XmlDocument { XmlResolver = null };
                            contentXml.LoadXml(text);
                            XmlNode specificNode = contentXml.SelectSingleNode(dataPath);
                            if (specificNode != null)
                            {
                                return specificNode.InnerText; // Retourne le contenu du nœud
                            }

                            return text;
                            /*

                            // this.businessControllerProvider = Globals..GetRequiredService<IBusinessControllerProvider>();
                            XmlDocument moduleXml = new XmlDocument { XmlResolver = null };
                            XmlNode moduleNode = ModuleController.SerializeModule(moduleXml, module, true);
                            // var content =  moduleNode["content"].Value;


                            if (!string.IsNullOrEmpty(XmlUtils.GetNodeValue(moduleNode.CreateNavigator(), "content")))
                            {
                                string content = moduleNode.SelectSingleNode("content").InnerXml;
                                content = content.Substring(9, content.Length - 12);
                                var decodedContent = WebUtility.HtmlDecode(content);

                                XmlDocument contentXml = new XmlDocument { XmlResolver = null };
                                contentXml.LoadXml(decodedContent);

                                // Sélectionner un nœud spécifique par chemin XPath
                                XmlNode specificNode = contentXml.SelectSingleNode(dataPath);
                                if (specificNode != null)
                                {
                                    return specificNode.InnerText; // Retourne le contenu du nœud
                                }
                                return decodedContent;
                            }

                            //StringWriter sw = new StringWriter();
                            //XmlTextWriter xw = new XmlTextWriter(sw);
                            //moduleNode.WriteTo(xw);
                            //var content = sw.ToString();

                            //return content;
                            */
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
