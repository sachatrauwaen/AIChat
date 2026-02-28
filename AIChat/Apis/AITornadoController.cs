using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Services;
using Dnn.PersonaBar.Library;
using Dnn.PersonaBar.Library.Attributes;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Instrumentation;
using DotNetNuke.Web.Api;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using Satrabel.AIChat.History;
using Satrabel.AIChat.Services;
using Satrabel.PersonaBar.AIChat.Apis.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace Satrabel.PersonaBar.AIChat.Apis
{

    public class Constants
    {
        public const string MenuName = "Dnn.AIChat";
        public const string Edit = "EDIT";
    }

    [MenuPermission(MenuName = Constants.MenuName)]
    public class AITornadoController : PersonaBarApiController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(AITornadoController));

        private readonly IMcpRegistry _mcpRegistry;

        private readonly IServiceProvider _dependencyProvider;

        private const string APIKEY_SETTING = "AIChat_ApiKey";
        private const string MODEL_SETTING = "AIChat_Model";
        private const string MAX_TOKENS_SETTING = "AIChat_MAX_TOKENS";
        private const string HISTORY_MAX_TOKENS_SETTING = "AIChat_HistoryMaxTokens";
        private const string HISTORY_MAX_TURNS_SETTING = "AIChat_HistoryMaxTurns";
        private const string TOOLS_SETTING = "AIChat_Tools";
        private const string AUTO_READONLY_TOOLS_SETTING = "AIChat_AUTO_READONLY_TOOLS";
        private const string AUTO_WRITE_TOOLS_SETTING = "AIChat_AUTO_WRITE_TOOLS";
        private const int MAX_TOKENS_DEFAULT = 4096;

        public AITornadoController(IMcpRegistry mcpRegistry, IServiceProvider dependencyProvider)
        {
            _mcpRegistry = mcpRegistry;
            _dependencyProvider = dependencyProvider;
        }

        private string GetApiKey()
        {
            return PortalController.GetPortalSetting(APIKEY_SETTING, PortalId, "");
        }

        private string GetModel()
        {
            return PortalController.GetPortalSetting(MODEL_SETTING, PortalId, "claude-3-7-sonnet-latest");
        }

        private int GetMaxTokens()
        {
            var value = PortalController.GetPortalSetting(MAX_TOKENS_SETTING, PortalId, MAX_TOKENS_DEFAULT.ToString());
            int.TryParse(value, out var maxTokens);
            return maxTokens > 0 ? maxTokens : MAX_TOKENS_DEFAULT;
        }

        private string GetConversationsFolder()
        {
            var portalPath = PortalSettings.HomeSystemDirectoryMapPath;
            return Path.Combine(portalPath, "aichat", "conversations");
        }

        private string GetPendingConversationCacheKey(string conversationId)
        {
            return $"AIChat_PendingConversation_{PortalId}_{conversationId}";
        }

        private Conversation GetPendingConversation(string conversationId)
        {
            return DataCache.GetCache(GetPendingConversationCacheKey(conversationId)) as Conversation;
        }

        private void SavePendingConversation(string conversationId, Conversation conversation)
        {
            DataCache.SetCache(GetPendingConversationCacheKey(conversationId), conversation);
        }

        private void RemovePendingConversation(string conversationId)
        {
            DataCache.RemoveCache(GetPendingConversationCacheKey(conversationId));
        }

        private ChatHistoryManager GetHistoryManager()
        {
            return new ChatHistoryManager(GetConversationsFolder());
        }

        private ChatHistoryPolicy BuildHistoryPolicy()
        {
            var policy = ChatHistoryPolicy.CreateDefault();

            var historyTokensValue = PortalController.GetPortalSetting(
                HISTORY_MAX_TOKENS_SETTING,
                PortalId,
                policy.MaxContextTokens.ToString());

            if (int.TryParse(historyTokensValue, out var historyTokens) && historyTokens > 0)
            {
                policy.MaxContextTokens = historyTokens;
            }

            var historyTurnsValue = PortalController.GetPortalSetting(
                HISTORY_MAX_TURNS_SETTING,
                PortalId,
                policy.MaxUserTurns.ToString());

            if (int.TryParse(historyTurnsValue, out var historyTurns) && historyTurns > 0)
            {
                policy.MaxUserTurns = historyTurns;
            }

            return policy;
        }

        private string GenerateSystemPrompt(TornadoChatRequest request)
        {
            var application = DotNetNuke.Application.DotNetNukeContext.Current.Application;
            var portalCount = PortalController.Instance.GetPortals().Count;
            var isHost = UserController.Instance.GetCurrentUserInfo()?.IsSuperUser ?? false;
            var ps = PortalSettings;

            var hostContext = $"<host>Version = v.{Globals.FormatVersion(application.Version, true)}, Product = {application.Description}, PortalCount = {portalCount}, Framework = {(isHost ? Globals.NETFrameworkVersion.ToString() : string.Empty)} </host>";
            var portalContext = $"<portal>PortalName = {ps.PortalName}, DefaultPortalAlias = {ps.DefaultPortalAlias}, DefaultLanguage = {ps.DefaultLanguage}</portal>";

            var airules = string.Empty;
            var rulesPath = ps.HomeSystemDirectoryMapPath + "airules";
            if (Directory.Exists(rulesPath))
            {
                var globalFile = Path.Combine(rulesPath, "global.md");
                if (File.Exists(globalFile))
                {
                    airules = "<global>" + File.ReadAllText(globalFile) + "</global>";
                }
                if (!string.IsNullOrEmpty(request.Rules))
                {
                    var rulePath = Path.Combine(rulesPath, request.Rules + ".md");
                    if (File.Exists(rulePath))
                    {
                        airules += $"\n<{request.Rules}>" + File.ReadAllText(rulePath) + $"</{request.Rules}>";
                    }
                }
            }

            return $"Output always in markdown format. When using tools, please use them one at a time and wait for results before making additional tool calls.\n{hostContext}\n{portalContext}\n{airules}";
        }

        private decimal CalculatePrice(string model, int inputTokens, int outputTokens)
        {
            var m = model.ToLower();
            decimal inputRate = 0m, outputRate = 0m;
            if (m.Contains("sonnet"))
            {
                inputRate = 3m;
                outputRate = 15m;
            }
            else if (m.Contains("haiku"))
            {
                inputRate = 0.8m;
                outputRate = 4m;
            }
            else if (m.Contains("opus"))
            {
                inputRate = 15m;
                outputRate = 75m;
            }
            return inputTokens * inputRate / 1_000_000m + outputTokens * outputRate / 1_000_000m;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<TornadoChatResponse> Chat(TornadoChatRequest request)
        {
            //DataCache.SetCache()
            if (string.IsNullOrEmpty(GetApiKey()))
            {
                return new TornadoChatResponse
                {
                    Success = false,
                    Message = "ApiKey missing (goto settings)"
                };
            }

            try
            {
                var historyManager = GetHistoryManager();
                ChatConversation chatHistory;
                ChatHistoryUsage historyUsage;

                if (!string.IsNullOrEmpty(request.ConversationId))
                {
                    chatHistory = historyManager.LoadConversation(request.ConversationId) ??
                                   historyManager.CreateConversation();
                }
                else
                {
                    chatHistory = historyManager.CreateConversation();
                }

                var historyPolicy = BuildHistoryPolicy();
                var contextMessages = historyManager.GetContextMessages(chatHistory.Id, historyPolicy, out historyUsage);

                TornadoToolsService toolsService = CreateToolService();
                var enabledToolNames = PortalController.GetPortalSetting(TOOLS_SETTING, PortalId, "")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                List<Tool> tools = null;
                if (request.Mode == "readonly")
                {
                    tools = toolsService.GetReadOnlyTools().Where(t => enabledToolNames.Contains(t.ResolvedName)).ToList();
                }
                else if (request.Mode == "agent")
                {
                    tools = toolsService.GetAllTools().Where(t => enabledToolNames.Contains(t.ResolvedName)).ToList();
                }

                // tools = toolsService.GetAllTools();

                var autoReadonlyTools = bool.Parse(PortalController.GetPortalSetting(AUTO_READONLY_TOOLS_SETTING, PortalId, "false"));
                var autoWriteTools = bool.Parse(PortalController.GetPortalSetting(AUTO_WRITE_TOOLS_SETTING, PortalId, "false"));


                var model = GetModel();
                var tornadoService = new TornadoChatService(GetApiKey(), Logger, model, GetMaxTokens());
                var systemPrompt = GenerateSystemPrompt(request);

                Conversation conv;

                if (request.RunTool && !string.IsNullOrEmpty(request.ToolName))
                {
                    // Tool confirmation/rejection: try to reuse the cached Conversation with proper tool_use state
                    var existingConversation = GetPendingConversation(chatHistory.Id);
                    if (existingConversation != null)
                    {
                        RemovePendingConversation(chatHistory.Id);
                        conv = existingConversation;
                    }
                    else
                    {
                        conv = tornadoService.CreateConversation(systemPrompt, tools);
                        ReplayHistory(conv, contextMessages);
                    }

                    var toolCallId = request.ToolCallId ?? request.ToolName;

                    if (request.ToolApproved)
                    {
                        var toolResult = await toolsService.ExecuteToolAsync(request.ToolName, request.ToolArguments);
                        conv.AddToolMessage(toolCallId, toolResult, true);
                        chatHistory.AddMessage(new Satrabel.AIChat.History.ChatMessage
                        {
                            Role = MessageRole.Tool,
                            ToolName = request.ToolName,
                            ToolArguments = request.ToolArguments,
                            ToolCallId = toolCallId,
                            Content = toolResult
                        });
                        chatHistory.AddMessage(MessageRole.User, $"[Tool Result for {request.ToolName}]: {toolResult}");
                    }
                    else
                    {
                        conv.AddToolMessage(toolCallId, "The user refused this action.", false);
                        chatHistory.AddMessage(new Satrabel.AIChat.History.ChatMessage
                        {
                            Role = MessageRole.Tool,
                            ToolName = request.ToolName,
                            ToolArguments = request.ToolArguments,
                            ToolCallId = toolCallId,
                            Content = "The user refused this action."
                        });
                        chatHistory.AddMessage(MessageRole.User, $"[Tool Result for {request.ToolName}]: The user refused this action.");

                        // Tool refused: return without calling the LLM
                        historyManager.SaveConversation(chatHistory.Id);
                        var refusedMessages = chatHistory.Messages.Select(m => new TornadoMessageDto
                        {
                            Role = m.Role.ToString().ToLowerInvariant(),
                            Content = m.Content,
                            ToolName = m.ToolName,
                            ToolArguments = m.ToolArguments,
                            ToolCallId = m.ToolCallId
                        }).ToList();
                        return new TornadoChatResponse
                        {
                            Success = true,
                            Message = string.Empty,
                            ConversationId = chatHistory.Id,
                            Messages = refusedMessages,
                            ToolCall = null,
                            TotalInputTokens = 0,
                            TotalOutputTokens = 0,
                            TotalPrice = 0
                        };
                    }
                }
                else
                {
                    conv = tornadoService.CreateConversation(systemPrompt, tools);
                    ReplayHistory(conv, contextMessages);

                    if (!string.IsNullOrWhiteSpace(request.Message))
                    {
                        conv.AppendUserInput(request.Message);
                        chatHistory.AddMessage(MessageRole.User, request.Message);

                        if (chatHistory.Messages.Count == 1)
                        {
                            var titleText = request.Message.Length > 60
                                ? request.Message.Substring(0, 60) + "..."
                                : request.Message;
                            chatHistory.Title = titleText;
                        }
                    }
                }

                int totalInputTokens = 0;
                int totalOutputTokens = 0;
                TornadoToolCallDto pendingToolCall = null;
                const int maxIterations = 10;

                for (int iteration = 0; iteration < maxIterations; iteration++)
                {
                    FunctionCall capturedCall = null;
                    bool shouldAutoExecute = false;

                    var response = await conv.GetResponseRich(async calls =>
                    {
                        var call = calls.FirstOrDefault();
                        if (call == null) return;

                        capturedCall = call;
                        shouldAutoExecute = autoReadonlyTools && toolsService.IsReadOnly(call.Name)
                                            || autoWriteTools && !toolsService.IsReadOnly(call.Name);
                    });

                    var usage = conv.MostRecentApiResult?.Usage;
                    totalInputTokens += usage?.PromptTokens ?? 0;
                    totalOutputTokens += usage?.CompletionTokens ?? 0;

                    if (capturedCall != null)
                    {
                        var assistantText = response?.Text;
                        if (!string.IsNullOrWhiteSpace(assistantText))
                        {
                            chatHistory.AddMessage(MessageRole.Assistant, assistantText);
                        }

                        if (shouldAutoExecute)
                        {
                            var lastMsg = conv.Messages.LastOrDefault();
                            if (lastMsg != null && lastMsg.Role == ChatMessageRoles.Tool)
                            {
                                conv.RemoveMessage(lastMsg);
                            }

                            var autoArgs = ParseToolArguments(capturedCall.Arguments);
                            var toolResult = await toolsService.ExecuteToolAsync(capturedCall.Name, autoArgs);

                            conv.AddToolMessage(capturedCall.ToolCall?.Id ?? capturedCall.Name, toolResult, true);

                            chatHistory.AddMessage(new Satrabel.AIChat.History.ChatMessage
                            {
                                Role = MessageRole.Tool,
                                ToolName = capturedCall.Name,
                                ToolArguments = autoArgs,
                                ToolCallId = capturedCall.ToolCall?.Id ?? capturedCall.Name,
                                Content = toolResult
                            });
                            chatHistory.AddMessage(MessageRole.User,
                                $"[Tool Result for {capturedCall.Name}]: {toolResult}");
                            continue;
                        }
                        else
                        {
                            // Remove the automatic tool result message appended by GetResponseRich
                            var lastMsg = conv.Messages.LastOrDefault();
                            if (lastMsg != null && lastMsg.Role == ChatMessageRoles.Tool)
                            {
                                conv.RemoveMessage(lastMsg);
                            }

                            SavePendingConversation(chatHistory.Id, conv);

                            var parsedArgs = ParseToolArguments(capturedCall.Arguments);
                            pendingToolCall = new TornadoToolCallDto
                            {
                                Id = capturedCall.ToolCall?.Id ?? capturedCall.Name,
                                Name = capturedCall.Name,
                                Arguments = parsedArgs,
                                ReadOnly = toolsService.IsReadOnly(capturedCall.Name),
                                Description = capturedCall.Arguments
                            };
                            break;
                        }
                    }
                    else
                    {
                        var text = response?.Text ?? string.Empty;
                        chatHistory.AddMessage(MessageRole.Assistant, text);
                        break;
                    }
                }

                historyManager.SaveConversation(chatHistory.Id);

                if (historyUsage != null)
                {
                    Logger.Info(
                        $"AIChat history usage for conversation {chatHistory.Id}: " +
                        $"kept {historyUsage.KeptMessages}/{historyUsage.TotalMessages} messages, " +
                        $"~{historyUsage.KeptApproxTokens}/{historyUsage.TotalApproxTokens} tokens, " +
                        $"tool messages {historyUsage.KeptToolMessages}/{historyUsage.TotalToolMessages}.");
                }

                var messages = chatHistory.Messages.Select(m => new TornadoMessageDto
                {
                    Role = m.Role.ToString().ToLowerInvariant(),
                    Content = m.Content,
                    ToolName = m.ToolName,
                    ToolArguments = m.ToolArguments,
                    ToolCallId = m.ToolCallId
                }).ToList();

                return new TornadoChatResponse
                {
                    Success = true,
                    Message = string.Empty,
                    ConversationId = chatHistory.Id,
                    Messages = messages,
                    ToolCall = pendingToolCall,
                    TotalInputTokens = totalInputTokens,
                    TotalOutputTokens = totalOutputTokens,
                    TotalPrice = CalculatePrice(model, totalInputTokens, totalOutputTokens)
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Error in AITornadoController.Chat", ex);
                return new TornadoChatResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        private TornadoToolsService CreateToolService()
        {
            ModuleInitializer.Initialize(_mcpRegistry, _dependencyProvider);
            var toolsService = new TornadoToolsService(_mcpRegistry, _dependencyProvider);
            return toolsService;
        }

        private void ReplayHistory(Conversation conv, IEnumerable<Satrabel.AIChat.History.ChatMessage> messages)
        {
            if (messages == null)
            {
                return;
            }

            foreach (var m in messages)
            {
                if (m.Role == MessageRole.Tool)
                    continue;
                if (ChatHistoryHelpers.IsToolResultWrapper(m))
                    continue;
                if (m.Role == MessageRole.User)
                    conv.AppendUserInput(m.Content);
                else if (m.Role == MessageRole.Assistant)
                    conv.AppendExampleChatbotOutput(m.Content);
            }
        }

        private Dictionary<string, object> ParseToolArguments(string argsJson)
        {
            if (string.IsNullOrEmpty(argsJson)) return null;
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(argsJson);
        }

        [HttpGet]
        public IEnumerable<object> GetConversations()
        {
            var historyManager = GetHistoryManager();
            historyManager.LoadAllConversationsAsync();
            return historyManager.GetAllConversations()
                .Select(c => new
                {
                    id = c.Id,
                    title = c.Title,
                    createdAt = c.CreatedAt,
                    lastModified = c.LastModified
                });
        }

        [HttpGet]
        public TornadoChatResponse LoadConversation(string id)
        {
            var historyManager = GetHistoryManager();
            var conversation = historyManager.LoadConversation(id);

            if (conversation == null)
            {
                return new TornadoChatResponse
                {
                    Success = false,
                    Message = "Conversation not found"
                };
            }

            var messages = conversation.Messages.Select(m => new TornadoMessageDto
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content,
                ToolName = m.ToolName,
                ToolArguments = m.ToolArguments,
                ToolCallId = m.ToolCallId
            }).ToList();

            return new TornadoChatResponse
            {
                Success = true,
                Message = string.Empty,
                ConversationId = conversation.Id,
                Messages = messages
            };
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public TornadoDeleteConversationResult DeleteConversation(TornadoDeleteConversationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Id))
            {
                return new TornadoDeleteConversationResult()
                {
                    Success = true,
                    Message = "Conversation id is required"
                };
            }

            var folder = GetConversationsFolder();
            var filePath = Path.Combine(folder, $"{request.Id}.json");
            if (!File.Exists(filePath))
            {
                return new TornadoDeleteConversationResult()
                {
                    Success = true,
                    Message = "Conversation not found"
                };
            }
            File.Delete(filePath);
            RemovePendingConversation(request.Id);
            return new TornadoDeleteConversationResult()
            {
                Success = true,
                Message = "Conversation deleted"
            };
        }

         [HttpGet]
        public async Task<SettingsDto> GetSettings()
        {
            var res = new SettingsDto();
            try
            {
                var apiKey = PortalController.GetPortalSetting(APIKEY_SETTING, PortalId, "");
                var models = ChatModel.AllModels;
                res.Models = new List<ModelDto>();
                foreach (var model in models)
                {
                    res.Models.Add(new ModelDto
                    {
                        Value = model.Name,
                        Name = $"{model.Provider.ToString()} - {model.Name}" 
                    });
                }
               
                res.ApiKey = string.IsNullOrEmpty(apiKey) ? "" : "********";
                res.MaxTokens = int.Parse(PortalController.GetPortalSetting(MAX_TOKENS_SETTING, PortalId, MAX_TOKENS_DEFAULT.ToString()));
                res.HistoryMaxTokens = int.Parse(PortalController.GetPortalSetting(HISTORY_MAX_TOKENS_SETTING, PortalId, "4096"));
                res.HistoryMaxTurns = int.Parse(PortalController.GetPortalSetting(HISTORY_MAX_TURNS_SETTING, PortalId, "20"));
                res.Model = PortalController.GetPortalSetting(MODEL_SETTING, PortalId, ChatModel.Anthropic.Claude4.Sonnet250514);
                res.AutoReadonlyTools = bool.Parse(PortalController.GetPortalSetting(AUTO_READONLY_TOOLS_SETTING, PortalId, "false"));
                res.AutoWriteTools = bool.Parse(PortalController.GetPortalSetting(AUTO_WRITE_TOOLS_SETTING, PortalId, "false"));
                var tools = PortalController.GetPortalSetting(TOOLS_SETTING, PortalId, "").Split(',').ToList();
                var toolsService = CreateToolService();
                res.Tools = toolsService.GetAllTools().Select(t => new ToolDto
                {
                    Name = t.ResolvedName,
                    Description = t.ResolvedDescription,
                    Active = tools.Contains(t.ResolvedName) || string.IsNullOrEmpty(GetApiKey()), // if no API key, all tools are active
                }).ToList();

                var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "airules";

                if (Directory.Exists(rulesPath))
                {
                    res.Rules = Directory.GetFiles(rulesPath).Where(f => !f.EndsWith("global.md")).Select(f => new RuleDto
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        Rule = File.ReadAllText(f)
                    }).ToList();
                    var globalFilename = Path.Combine(rulesPath, "global.md");
                    if (File.Exists(globalFilename))
                    {
                        res.GlobalRules = File.ReadAllText(globalFilename);
                    }
                }
                else
                {
                    Directory.CreateDirectory(rulesPath);

                }
                if (string.IsNullOrEmpty(res.GlobalRules))
                {
                    res.GlobalRules = "# global rules";
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
            }
            if (!string.IsNullOrEmpty(request.Model))
            {
                PortalController.UpdatePortalSetting(PortalId, MODEL_SETTING, request.Model);
            }
            PortalController.UpdatePortalSetting(PortalId, AUTO_READONLY_TOOLS_SETTING, request.AutoReadonlyTools.ToString());
            PortalController.UpdatePortalSetting(PortalId, AUTO_WRITE_TOOLS_SETTING, request.AutoWriteTools.ToString());
            if (request.Tools != null)
            {
                PortalController.UpdatePortalSetting(PortalId, TOOLS_SETTING, string.Join(",", request.Tools.Where(t => t.Active).Select(t => t.Name)));
            }
            PortalController.UpdatePortalSetting(PortalId, MAX_TOKENS_SETTING, request.MaxTokens.ToString());
            PortalController.UpdatePortalSetting(PortalId, HISTORY_MAX_TOKENS_SETTING, request.HistoryMaxTokens.ToString());
            PortalController.UpdatePortalSetting(PortalId, HISTORY_MAX_TURNS_SETTING, request.HistoryMaxTurns.ToString());
            var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "airules";
            File.WriteAllText(Path.Combine(rulesPath, "global.md"), request.GlobalRules);
            if (!Directory.Exists(rulesPath))
            {
                Directory.CreateDirectory(rulesPath);
            }
            var files = Directory.GetFiles(rulesPath).Where(f => !f.EndsWith("global.md"));
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
            var toolsService = CreateToolService();

            var folders = new List<string>();
            //foreach (var tool in toolsService.GetAllTools())
            //{
            //    var rulesFolder = toolsService.GetToolFolder(tool.Name);
            //    if (!string.IsNullOrEmpty(rulesFolder) && !folders.Contains(rulesFolder))
            //    {
            //        folders.Add(rulesFolder);
            //    }
            //}
            foreach (var folder in folders)
            {
                res.Rules.AddRange(GetRules(AppDomain.CurrentDomain.BaseDirectory + folder.Replace("/", "\\").Trim('\\')));
            }
            var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "airules";
            res.Rules.AddRange(GetRules(rulesPath));
            res.AutoReadonlyTools = bool.Parse(PortalController.GetPortalSetting(AUTO_READONLY_TOOLS_SETTING, PortalId, "false"));
            res.AutoWriteTools = bool.Parse(PortalController.GetPortalSetting(AUTO_WRITE_TOOLS_SETTING, PortalId, "false"));
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
