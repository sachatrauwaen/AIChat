using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Services;
using Dnn.PersonaBar.Library;
using Dnn.PersonaBar.Library.Attributes;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Instrumentation;
using DotNetNuke.Web.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;


namespace Satrabel.PersonaBar.DnnMcp.Apis
{
    public class Constants
    {
        public const string MenuName = "Dnn.DnnMcp";
        public const string Edit = "EDIT";
    }

    [MenuPermission(MenuName = Constants.MenuName)]
    public partial class DnnMcpController : PersonaBarApiController
    {

        private readonly IMcpRegistry _mcpRegistry;
        private readonly IServiceProvider _dependencyProvider;

        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(DnnMcpController));

        // private readonly string _apiKey = "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"; // API key from env/config
        public const string APIKEY_SETTING = "DnnMcp_ApiKey";
        private const string APIKEY_VALID_DELAY_SETTING = "DnnMcp_ApiKeyValidDelay";
        public const string APIKEY_ACTIVE_SETTING = "DnnMcp_ApiKeyActive";
        public const string APIKEY_VALID_UNTIL_DATE_SETTING = "DnnMcp_ApiKeyValidUntilDate";
        public const string APIKEY_CREATED_BY_SETTING = "DnnMcp_ApiKeyCreatedBy";
        
        public const string TOOLS_SETTING = "DnnMcp_Tools";

        public DnnMcpController(IMcpRegistry mcpRegistry, IServiceProvider dependencyProvider)
        {
            _mcpRegistry = mcpRegistry;
            _dependencyProvider = dependencyProvider;
        }

        [HttpGet]
        public async Task<SettingsDto> GetSettings()
        {
            ModuleInitializer.Initialize(_mcpRegistry, _dependencyProvider);
            var res = new SettingsDto();
            try
            {
                var apiKey = PortalController.GetPortalSetting(APIKEY_SETTING, PortalId, "");
               
                res.ApiKey = string.IsNullOrEmpty(apiKey) ? "" : "********";
                res.ApiKeyValidDelay = int.Parse(PortalController.GetPortalSetting(APIKEY_VALID_DELAY_SETTING, PortalId, "90"));
                res.ApiKeyValidDelayActive = bool.Parse(PortalController.GetPortalSetting(APIKEY_ACTIVE_SETTING, PortalId, "true"));
                res.ApiKeyValidUntilDate = PortalController.GetPortalSetting(APIKEY_VALID_UNTIL_DATE_SETTING, PortalId, "");
                var tools = PortalController.GetPortalSetting(TOOLS_SETTING, PortalId, "").Split(',').ToList();
                res.Tools = _mcpRegistry.GetAllTools().Select(t => new ToolDto
                {
                    Name = t.Name,
                    Description = t.Description,
                    Active = tools.Contains(t.Name),
                    Category = t.Category,
                }).ToList();

                var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "mcp/prompts";

                if (Directory.Exists(rulesPath))
                {
                    res.Rules = Directory.GetFiles(rulesPath).Select(f => new RuleDto
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        Rule = File.ReadAllText(f)
                    }).ToList();                   
                }
                else
                {
                    Directory.CreateDirectory(rulesPath);
                }
                
                res.Success = true;
            }
            catch (Exception ex)
            {
                res.Message = ex.Message;
                res.Success = false;
                Logger.Error("Error getting settings", ex);
            }

            return res;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public void SaveSettings(SettingsDto request)
        {
            if (!string.IsNullOrEmpty(request.ApiKey) && request.ApiKey != "********")
            {
                PortalController.UpdatePortalSetting(PortalId, APIKEY_SETTING, request.ApiKey);
                // Save the user ID who created/updated the API key
                PortalController.UpdatePortalSetting(PortalId, APIKEY_CREATED_BY_SETTING, UserInfo.UserID.ToString());
            }
            
            // Save the valid until date from the request
            if (!string.IsNullOrEmpty(request.ApiKeyValidUntilDate))
            {
                PortalController.UpdatePortalSetting(PortalId, APIKEY_VALID_UNTIL_DATE_SETTING, request.ApiKeyValidUntilDate);
            }
            
            PortalController.UpdatePortalSetting(PortalId, APIKEY_VALID_DELAY_SETTING, request.ApiKeyValidDelay.ToString());
            PortalController.UpdatePortalSetting(PortalId, APIKEY_ACTIVE_SETTING, request.ApiKeyValidDelayActive.ToString());
            if (request.Tools != null)
            {
                PortalController.UpdatePortalSetting(PortalId, TOOLS_SETTING, string.Join(",", request.Tools.Where(t => t.Active).Select(t => t.Name)));
            }
            var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "mcp/prompts";
            
            if (!Directory.Exists(rulesPath))
            {
                Directory.CreateDirectory(rulesPath);
            }
            var files = Directory.GetFiles(rulesPath);
            foreach (var file in files)
            {
                if (!request.Rules.Any(r => r.Name == Path.GetFileNameWithoutExtension(file)))
                {
                    File.Delete(file);
                }
            }
            foreach (var rule in request.Rules)
            {
                File.WriteAllText(Path.Combine(rulesPath, rule.Name + ".md"), rule.Rule);
            }
        }

        [HttpGet]
        public async Task<InfoDto> GetInfo()
        {
            var res = new InfoDto()
            {
                Rules = new List<string>(),
            };
            //ToolsService toolsService = new ToolsService(Logger);
            //var folders = new List<string>();
            //foreach (var tool in toolsService.GetAllTools())
            //{
            //    var rulesFolder = toolsService.GetToolFolder(tool.Name);
            //    if (!string.IsNullOrEmpty(rulesFolder) && !folders.Contains(rulesFolder))
            //    {
            //        folders.Add(rulesFolder);
            //    }
            //}
            //foreach (var folder in folders)
            //{
            //    res.Rules.AddRange(GetRules(AppDomain.CurrentDomain.BaseDirectory + folder.Replace("/", "\\").Trim('\\')));
            //}
            var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "airules";
            res.Rules.AddRange(GetRules(rulesPath));
            res.Success = true;
            return res;
        }

        private List<string> GetRules(string folder)
        {
            var res = new List<string>();
            if (Directory.Exists(folder))
            {
                res.AddRange(Directory.GetFiles(folder)
                    .Where(f => !f.EndsWith("global.md"))
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToList());
            }
            return res;
        }

       
    }
}