using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DotNetNuke.Web.Api;
using Dnn.PersonaBar.Library;
using Dnn.PersonaBar.Library.Attributes;
using System;
using AnthropicClient;
using AnthropicClient.Models;
using System.Net.Http;
using Satrabel.AIChat.Tools;
using Satrabel.AIChat.Apis;
using Satrabel.PersonaBar.AIChat.Apis.Dto;
using DotNetNuke.Collections;
using DotNetNuke.Entities.Host;
using DotNetNuke.Entities.Users;
using System.Web;
using DotNetNuke.Common;
using System.IO;
using System.Web.UI.HtmlControls;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Instrumentation;
using Satrabel.AIChat.Services;


namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class Constants
    {
        public const string MenuName = "Dnn.AIChat";
        public const string Edit = "EDIT";
    }

    [MenuPermission(MenuName = Constants.MenuName)]
    public partial class AIChatController : PersonaBarApiController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(AIChatController));

        // private readonly string _apiKey = "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"; // API key from env/config
        private const string APIKEY_SETTING = "AIChat_ApiKey";
        private const string MODEL_SETTING = "AIChat_Model";

        [HttpGet]
        public async Task<SettingsDto> GetSettings()
        {
            var res = new SettingsDto();
            if (!string.IsNullOrEmpty(GetApiKey()))
            {
                AnthropicService anthropicService = new AnthropicService(GetApiKey(), Logger, AnthropicModels.Claude35Haiku20241022);
                res.Models = await anthropicService.GetModelsAsync();
            }
            else
            {
                res.Models = new List<ModelDto> { new ModelDto { Value = AnthropicModels.Claude35Sonnet20241022, Name = "Claude 3.5 Sonnet" } };
            }
            res.ApiKey = PortalController.GetPortalSetting(APIKEY_SETTING, PortalId, "");
            res.Model = PortalController.GetPortalSetting(MODEL_SETTING, PortalId, AnthropicModels.Claude35Sonnet20241022);

            var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "airules";
            res.GlobalRules = File.ReadAllText(Path.Combine(PortalSettings.Current.HomeSystemDirectoryMapPath, "airules.md"));
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
            return res;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public void SaveSettings(SettingsDto request)
        {
            if (!string.IsNullOrEmpty(request.ApiKey))
            {
                PortalController.UpdatePortalSetting(PortalId, APIKEY_SETTING, request.ApiKey);
            }
            if (!string.IsNullOrEmpty(request.Model))
            {
                PortalController.UpdatePortalSetting(PortalId, MODEL_SETTING, request.Model);
            }
            File.WriteAllText(Path.Combine(PortalSettings.Current.HomeSystemDirectoryMapPath, "airules.md"), request.GlobalRules);
            var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "airules";
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
            var res = new InfoDto();

            var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "airules";
            if (Directory.Exists(rulesPath))
            {
                res.Rules = Directory.GetFiles(rulesPath)
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToList();
            }
            res.Success = true;

            /*
            AnthropicService anthropicService = new AnthropicService(GetApiKey(), Logger);
            res.Models = new List<ModelDto>(new ModelDto[] {
                new ModelDto{
                    Name= "Sonnet 3.5",
                    Value =AnthropicModels.Claude35SonnetLatest
                },
                new ModelDto{
                    Name= "Haiku 3.5",
                    Value =AnthropicModels.Claude35HaikuLatest
                },
                new ModelDto{
                    Name= "Sonnet 3.7",
                    Value ="claude-3-7-sonnet-latest"
                },
            });
            */
            return res;
        }



        /// <summary>
        /// Chat endpoint that uses tools to perform calculations.
        /// </summary>
        /// <remarks>
        /// This endpoint demonstrates the use of tools with Claude AI.

        /// </remarks>
        /// <param name="request">Chat request containing the user message</param>
        /// <returns>Response with the AI's message after tool execution</returns>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ChatResponse> ChatWithTools(ChatToolRequest request)
        {
            if (string.IsNullOrEmpty(GetApiKey()))
            {
                return new ChatResponse() { 
                    Success = false,
                    Message = "ApiKey missing (goto settings)"
                };
            }

                ToolsService toolsService = new ToolsService(Logger);
            var model = PortalController.GetPortalSetting(MODEL_SETTING, PortalId, AnthropicModels.Claude35Sonnet20241022);

            AnthropicService anthropicService = new AnthropicService(GetApiKey(), Logger, model);
            ToolResponse toolResponse = null;
            try
            {
                var dtos = request.Messages;
                /*
                if (!request.RunTool && dtos.Any() && dtos.Last().AContent?.Any(c => c.Type == "tool_use") == true)
                {
                    dtos.RemoveAt(dtos.Count - 1);
                }
                */

                // Remove tool use results longer than 1000 chars
                foreach (var msg in dtos)
                {
                    if (msg.AContent != null)
                    {
                        var toolResults = msg.AContent.Where(c => c.Type == "tool_result").ToList();
                        foreach (var result in toolResults)
                        {
                            if (result.Result?.Length > 1000)
                            {
                                result.Result = result.Result.Substring(0, 1000) + "... [truncated]";
                            }
                        }
                    }
                }

                //List<Message> messages = request.Messages.Select(m => new Message(
                //  m.Role,
                //  GetContent(m)
                //)).ToList();

                string systemPrompt = GenerateSystemPrompt(request);

                AnthropicResult<MessageResponse> response;
                if (request.RunTool) // run tools
                {
                    var toolCall = toolsService.GetToolCall(request.ToolUse);
                    var tool = toolCall.ToolUse.ToText();
                    /*
                    var toolCallResult = await toolCall.InvokeAsync<string>();
                    string toolResultContent;

                    if (toolCallResult.IsSuccess && toolCallResult.Value != null)
                    {
                        Logger.Info(toolCallResult.Value);
                        toolResultContent = toolCallResult.Value;
                    }
                    else
                    {
                        Logger.Error(toolCallResult.Error.Message);
                        toolResultContent = toolCallResult.Error.Message;
                    }
                    */
                    var toolResultContent = await toolsService.InvokeAsync(toolCall);
                    //messages.Add(
                    //      new AnthropicClient.Models.Message(
                    //        MessageRole.User,
                    //        new List<Content> {
                    //          new ToolResultContent(
                    //              toolCall.ToolUse.Id,
                    //              toolResultContent
                    //            )}
                    //      )
                    //    );

                    dtos.Add(new MessageDto
                    {
                        Role = MessageRole.User,
                        Content = toolResultContent,
                        ContentType = "tool_result",
                        ToolName = toolCall.Tool.Name,
                        ToolFullname = tool,
                        AContent = new List<ContentDto> {
                              new ContentDto(){
                                  Type= "tool_result",
                                  Id= toolCall.ToolUse.Id,
                                  Result = toolResultContent
                              }
                        }
                    });
                    /*
                    response = await client.CreateMessageAsync(new MessageRequest(
                      AnthropicModels.Claude35Sonnet,
                      messages,
                      tools: GetTools(request, toolsService.GetReadOnlyTools(), toolsService.GetAllTools()),
                      system: systemPrompt
                    ));

                    if (!response.IsSuccess)
                    {
                        Logger.Error("Failed to create message");
                        Logger.ErrorFormat("Error Type: {0}", response.Error.Error.Type);
                        Logger.ErrorFormat("Error Message: {0}", response.Error.Error.Message);
                        throw new Exception($"Failed to create message: {response.Error.Error.Message}");
                    }
                    */

                    response = await anthropicService.CreateMessageAsync(
                         messages: dtos,
                         tools: GetTools(request, toolsService.GetReadOnlyTools(), toolsService.GetAllTools()),
                         system: systemPrompt
                    );

                    var inputTokens = response.Value.Usage.InputTokens;
                    var cacheCreationInputTokens = response.Value.Usage.CacheCreationInputTokens;
                    var cacheReadInputTokens = response.Value.Usage.CacheReadInputTokens;
                    var outputTokens = response.Value.Usage.OutputTokens;
                    decimal price = inputTokens * 3 / 1000000
                                    + outputTokens * 15 / 1000000
                                    + cacheCreationInputTokens * 3.75m / 1000000
                                    + cacheReadInputTokens * 0.3m / 1000000;

                    /*
                    foreach (var content in response.Value.Content)
                    {
                        switch (content)
                        {
                            case TextContent textContent:
                                Logger.Info(textContent.Text);
                                break;
                        }
                    }
                    */
                    // messages.Add(new Message(MessageRole.Assistant, response.Value.Content));
                    dtos.Add(new MessageDto
                    {
                        Role = MessageRole.Assistant,
                        Content = string.Join("\n", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.ToText())),
                        ContentType = string.Join(",", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.Type)),
                        AContent = response.Value.Content.Select(c => GetContentDto(c)).ToList(),
                        InputTokens = inputTokens,
                        OutputTokens = outputTokens,
                        CacheCreationInputTokens = cacheCreationInputTokens,
                        CacheReadInputTokens = cacheReadInputTokens,
                        Price = $"${price.ToString("0.00000")} ({inputTokens} input / {outputTokens} output tokens)"
                    });
                }
                else
                {
                    /*
                    response = await client.CreateMessageAsync(new MessageRequest(
                     AnthropicModels.Claude35Sonnet,
                     messages,
                     system: systemPrompt,
                     tools: GetTools(request, toolsService.GetReadOnlyTools(), toolsService.GetAllTools())
                   ));

                    if (!response.IsSuccess)
                    {
                        Logger.Error("Failed to create message");
                        Logger.ErrorFormat("Error Type: {0}", response.Error.Error.Type);
                        Logger.ErrorFormat("Error Message: {0}", response.Error.Error.Message);
                        throw new Exception($"Failed to create message: {response.Error.Error.Message}");
                    }
                    */
                    response = await anthropicService.CreateMessageAsync(
                        messages: dtos,
                        tools: GetTools(request, toolsService.GetReadOnlyTools(), toolsService.GetAllTools()),
                        system: systemPrompt
                    );

                    var inputTokens = response.Value.Usage.InputTokens;
                    var outputTokens = response.Value.Usage.OutputTokens;
                    var cacheCreationInputTokens = response.Value.Usage.CacheCreationInputTokens;
                    var cacheReadInputTokens = response.Value.Usage.CacheReadInputTokens;
                    decimal price = inputTokens * 3 / 1000000 + outputTokens * 15 / 1000000;
                    // messages.Add(new Message(MessageRole.Assistant, response.Value.Content));
                    dtos.Add(new MessageDto
                    {
                        Role = MessageRole.Assistant,
                        Content = string.Join("\n", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.ToText())),
                        ContentType = string.Join(",", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.Type)),
                        AContent = response.Value.Content.Select(c => GetContentDto(c)).ToList(),
                        InputTokens = inputTokens,
                        OutputTokens = outputTokens,
                        CacheCreationInputTokens = cacheCreationInputTokens,
                        CacheReadInputTokens = cacheReadInputTokens,
                        Price = $"${price.ToString("0.00000")} ({inputTokens} input / {outputTokens} output tokens)"
                    });
                    /*
                    foreach (var content in response.Value.Content)
                    {
                        switch (content)
                        {
                            case TextContent textContent:
                                Logger.Info(textContent.Text);
                                break;
                            case ToolUseContent toolUseContent:
                                Logger.Info(toolUseContent.Name);
                                break;
                        }
                    }
                    */
                }

                if (response.Value.ToolCall != null)
                {
                    toolResponse = new ToolResponse
                    {
                        Name = response.Value.ToolCall.Tool.Name,
                        Fullname = response.Value.ToolCall.ToolUse.ToText(),
                        ToolUse = response.Value.ToolCall.ToolUse,
                    };
                }

                var totalInputTokens = dtos.Sum(d => d.InputTokens);
                var totalOutputTokens = dtos.Sum(d => d.OutputTokens);
                var totalCacheCreationInputTokens = dtos.Sum(d => d.CacheCreationInputTokens);
                var totalCacheReadInputTokens = dtos.Sum(d => d.CacheReadInputTokens);
                decimal totalPrice = totalInputTokens * 3 / 1000000m
                                    + totalOutputTokens * 15 / 1000000m
                                    + totalCacheCreationInputTokens * 3.75m / 1000000m
                                    + totalCacheReadInputTokens * 0.3m / 1000000m;

                return new ChatResponse
                {
                    Messages = dtos,
                    Success = true,
                    Message = "",
                    Tool = toolResponse,
                    TotalPrice = totalPrice,
                    TotalInputTokens = totalInputTokens,
                    TotalOutputTokens = totalOutputTokens,
                    TotalCacheCreationInputTokens = totalCacheCreationInputTokens,
                    TotalCacheReadInputTokens = totalCacheReadInputTokens,
                };
            }
            catch (Exception ex)
            {
                return new ChatResponse
                {
                    Success = false,
                    Response = null,
                    Message = $"Error : {ex.Message}"
                };
            }
        }

        private string GenerateSystemPrompt(ChatToolRequest request)
        {
            var application = DotNetNuke.Application.DotNetNukeContext.Current.Application;
            var controlBarController = DotNetNuke.Web.Components.Controllers.ControlBarController.Instance;
            //var upgradeIndicator = controlBarController.GetUpgradeIndicator(application.Version, request.IsLocal, request.IsSecureConnection);
            var portalCount = DotNetNuke.Entities.Portals.PortalController.Instance.GetPortals().Count;
            var isHost = UserController.Instance.GetCurrentUserInfo()?.IsSuperUser ?? false;

            var hostContext = $"<host>Version = v.{Globals.FormatVersion(application.Version, true)}, Product = {application.Description}, PortalCount = {portalCount}, Framework = {(isHost ? Globals.NETFrameworkVersion.ToString() : string.Empty)} </host>";
            var ps = PortalSettings;
            var portalcontext = $"<portal>PortalName = {ps.PortalName}, DefaultPortalAlias = {ps.DefaultPortalAlias}, DefaultLanguage = {ps.DefaultLanguage}</portal>";

            var airulesFilename = ps.HomeSystemDirectoryMapPath + "airules.md";
            var airules = string.Empty;
            if (File.Exists(airulesFilename))
            {
                airules = File.ReadAllText(airulesFilename);
            }
            else
            {
                File.WriteAllText(airulesFilename, "# ai rules");
            }
            if (!string.IsNullOrEmpty(request.Rules))
            {
                var rulesPath = ps.HomeSystemDirectoryMapPath + "airules\\" + request.Rules + ".md";
                if (File.Exists(rulesPath))
                {
                    airules += "\n" + File.ReadAllText(rulesPath);
                }
            }
            var systemPrompt = $"{hostContext} \n {portalcontext} \n {airules}";
            return systemPrompt;
        }

        private string GetApiKey()
        {
            return PortalController.GetPortalSetting(APIKEY_SETTING, PortalId, "");

            //return _apiKey;
        }

        private static List<Tool> GetTools(ChatToolRequest request, List<Tool> readOnlyTools, List<Tool> allTools)
        {
            if (request.Mode == "readonly")
            {
                return readOnlyTools;
            }
            else if (request.Mode == "agent")
            {
                return allTools;
            }
            else
            {
                return new List<Tool>();
            }
        }

        private static ContentDto GetContentDto(Content content)
        {
            var res = new ContentDto();
            switch (content)
            {
                case TextContent textContent:
                    res.Type = "text";
                    res.Text = textContent.Text;
                    break;
                case ToolUseContent toolUseContent:
                    res.Type = "tool_use";
                    res.Id = toolUseContent.Id;
                    res.Name = toolUseContent.Name;
                    res.Input = toolUseContent.Input;
                    break;
                case ToolResultContent toolResultContent:
                    res.Type = "tool_result";
                    res.Id = toolResultContent.ToolUseId;
                    res.Result = toolResultContent.Content;
                    break;
            }

            return res;
        }



        /// <summary>
        /// Creates messages with the system instruction for markdown formatting based on user preferences
        /// </summary>
        private List<Services.Message> CreateMessagesWithMarkdownFormatting(List<MessageDto> messages, MarkdownPreferences preferences = null)
        {
            preferences = preferences ?? new MarkdownPreferences();

            string systemPrompt = "Please format your responses using Markdown. ";

            if (preferences.UseHeaders)
                systemPrompt += "Use headers (# for main headers, ## for subheaders) to organize information. ";

            if (preferences.UseBulletPoints)
                systemPrompt += "Use bullet points (- item) for lists. ";

            if (preferences.UseCodeBlocks)
                systemPrompt += "Use ```language code blocks for code or technical information. ";

            if (preferences.UseTables)
                systemPrompt += "Use markdown tables for tabular data. ";

            if (preferences.UseEmphasis)
                systemPrompt += "Use **bold** for emphasis and *italics* for definitions or important terms. ";

            systemPrompt += "Make your response clear, well-formatted, and easy to read.";

            var messagesList = messages.Select(m => new Services.Message
            {
                Role = m.Role,
                Content = m.Content,
                ContextText = m.Content,
            }).ToList();

            return messagesList;

            //return new List<Message>
            //{
            //    //new Message { role = "system", content = systemPrompt },
            //    new Message { Role = "user", Content = userMessage }
            //};
        }
    }
}