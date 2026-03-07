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
using LlmTornado.Models;
using Newtonsoft.Json;
using Satrabel.AIChat.History;
using Satrabel.AIChat.Services;
using Satrabel.PersonaBar.AIChat.Apis.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
        private const string DEBUG_SETTING = "AIChat_Debug";
        private const string SELECTED_MODE_SETTING = "AIChat_SelectedMode";
        private const string SELECTED_RULE_SETTING = "AIChat_SelectedRule";
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
                ChatHistoryPolicy.DefaultMaxContextTokens.ToString());

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
            SaveChatPreferences(request.Mode, request.Rules);
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

                    // Remove the placeholder for the current tool and replace with the real result
                    ReplaceToolPlaceholder(conv, toolCallId,
                        request.ToolApproved
                            ? await toolsService.ExecuteToolAsync(request.ToolName, request.ToolArguments)
                            : "The user refused this action.",
                        request.ToolApproved);

                    var toolResultContent = GetToolResultFromConv(conv, toolCallId);
                    chatHistory.AddMessage(new Satrabel.AIChat.History.ChatMessage
                    {
                        Role = MessageRole.Tool,
                        ToolName = request.ToolName,
                        ToolArguments = request.ToolArguments,
                        ToolCallId = toolCallId,
                        Content = toolResultContent
                    });
                    chatHistory.AddMessage(MessageRole.User, $"[Tool Result for {request.ToolName}]: {toolResultContent}");

                    // Check if there are more tools waiting for approval
                    var remaining = request.PendingToolCalls;
                    if (remaining != null && remaining.Count > 0)
                    {
                        // More tools need approval: return the next one without calling the LLM
                        SavePendingConversation(chatHistory.Id, conv);
                        historyManager.SaveConversation(chatHistory.Id);

                        var nextTool = remaining[0];
                        var restOfQueue = remaining.Count > 1 ? remaining.Skip(1).ToList() : null;

                        var queueMessages = chatHistory.Messages.Select(m => new TornadoMessageDto
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
                            Messages = queueMessages,
                            ToolCall = nextTool,
                            PendingToolCalls = restOfQueue,
                            TotalInputTokens = 0,
                            TotalOutputTokens = 0,
                            TotalPrice = 0
                        };
                    }

                    // No more pending tools. If the tool was refused and there are no remaining, skip LLM call.
                    if (!request.ToolApproved)
                    {
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

                    // All tools resolved, fall through to call the LLM
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
                List<TornadoToolCallDto> remainingPendingToolCalls = null;
                const int maxIterations = 10;

                for (int iteration = 0; iteration < maxIterations; iteration++)
                {
                    ChatRichResponse response;
                    try
                    {
                        response = await conv.GetResponseRich();
                    }
                    catch (Exception apiEx) when (apiEx.Message != null && apiEx.Message.Contains("rate_limit"))
                    {
                        Logger.Warn($"Rate limit hit on iteration {iteration}: {apiEx.Message}");
                        throw new InvalidOperationException(
                            $"Rate limit exceeded. Please wait a moment and try again. inputTokens: {totalInputTokens} / outputTokens: {totalOutputTokens}", apiEx);
                    }

                    var usage = conv.MostRecentApiResult?.Usage;
                    totalInputTokens += usage?.PromptTokens ?? 0;
                    totalOutputTokens += usage?.CompletionTokens ?? 0;

                    var functionBlocks = response?.Blocks?
                        .Where(b => b.Type == ChatRichResponseBlockTypes.Function && b.FunctionCall != null)
                        .ToList() ?? new List<ChatRichResponseBlock>();

                    if (functionBlocks.Count > 0)
                    {
                        var assistantText = response?.Text;
                        if (!string.IsNullOrWhiteSpace(assistantText))
                        {
                            chatHistory.AddMessage(MessageRole.Assistant, assistantText);
                        }

                        var autoExecute = new List<FunctionCall>();
                        var needsApproval = new List<FunctionCall>();

                        foreach (var block in functionBlocks)
                        {
                            var call = block.FunctionCall;
                            bool shouldAuto = (autoReadonlyTools && toolsService.IsReadOnly(call.Name))
                                           || (autoWriteTools && !toolsService.IsReadOnly(call.Name));
                            if (shouldAuto)
                                autoExecute.Add(call);
                            else
                                needsApproval.Add(call);
                        }

                        // Execute all auto-executable tools in parallel
                        if (autoExecute.Count > 0)
                        {
                            var autoTasks = autoExecute.Select(async call =>
                            {
                                var args = ParseToolArguments(call.Arguments);
                                var result = await toolsService.ExecuteToolAsync(call.Name, args);
                                return new { Call = call, Args = args, Result = result };
                            }).ToArray();

                            var autoResults = await Task.WhenAll(autoTasks);

                            foreach (var ar in autoResults)
                            {
                                var toolCallId = ar.Call.ToolCall?.Id ?? ar.Call.Name;
                                conv.AddToolMessage(toolCallId, ar.Result, true);
                                chatHistory.AddMessage(new Satrabel.AIChat.History.ChatMessage
                                {
                                    Role = MessageRole.Tool,
                                    ToolName = ar.Call.Name,
                                    ToolArguments = ar.Args,
                                    ToolCallId = toolCallId,
                                    Content = ar.Result
                                });
                                chatHistory.AddMessage(MessageRole.User,
                                    $"[Tool Result for {ar.Call.Name}]: {ar.Result}");
                            }
                        }

                        // Queue tools that need approval with placeholders
                        if (needsApproval.Count > 0)
                        {
                            foreach (var call in needsApproval)
                            {
                                var toolCallId = call.ToolCall?.Id ?? call.Name;
                                conv.AddToolMessage(toolCallId, "Pending user approval.", false);
                            }

                            var first = needsApproval[0];
                            pendingToolCall = new TornadoToolCallDto
                            {
                                Id = first.ToolCall?.Id ?? first.Name,
                                Name = first.Name,
                                Arguments = ParseToolArguments(first.Arguments),
                                ReadOnly = toolsService.IsReadOnly(first.Name),
                                Description = first.Arguments
                            };

                            if (needsApproval.Count > 1)
                            {
                                remainingPendingToolCalls = needsApproval.Skip(1).Select(call => new TornadoToolCallDto
                                {
                                    Id = call.ToolCall?.Id ?? call.Name,
                                    Name = call.Name,
                                    Arguments = ParseToolArguments(call.Arguments),
                                    ReadOnly = toolsService.IsReadOnly(call.Name),
                                    Description = call.Arguments
                                }).ToList();
                            }

                            SavePendingConversation(chatHistory.Id, conv);
                            break;
                        }

                        // All tools were auto-executed, continue the loop for the next LLM call
                        continue;
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

                IEnumerable<DebugMessageDto> debugMessages = null;
                var debugEnabled = bool.Parse(PortalController.GetPortalSetting(DEBUG_SETTING, PortalId, "false"));
                if (debugEnabled && conv?.Messages != null)
                {
                    debugMessages = conv.Messages.Select(m => new DebugMessageDto
                    {
                        Role = m.Role.ToString().ToLowerInvariant(),
                        Content = m.Content,
                        ToolCallId = m.ToolCallId,
                        ToolCalls = m.ToolCalls?.Select(t=>  t.Id).ToList(),
                        MessageTokens = m.GetMessageTokens(),
                    }).ToList();
                }

                return new TornadoChatResponse
                {
                    Success = true,
                    Message = string.Empty,
                    ConversationId = chatHistory.Id,
                    Messages = messages,
                    ToolCall = pendingToolCall,
                    PendingToolCalls = remainingPendingToolCalls,
                    TotalInputTokens = totalInputTokens,
                    TotalOutputTokens = totalOutputTokens,
                    TotalPrice = CalculatePrice(model, totalInputTokens, totalOutputTokens),
                    DebugMessages = debugMessages
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

        [ValidateAntiForgeryToken]
        [HttpPost]
        public HttpResponseMessage ChatStream(TornadoChatRequest request)
        {
            SaveChatPreferences(request.Mode, request.Rules);

            var apiKey = GetApiKey();
            var model = GetModel();
            var maxTokens = GetMaxTokens();
            var systemPrompt = GenerateSystemPrompt(request);
            var conversationsFolder = GetConversationsFolder();
            var enabledToolNames = PortalController.GetPortalSetting(TOOLS_SETTING, PortalId, "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var autoReadonlyTools = bool.Parse(PortalController.GetPortalSetting(AUTO_READONLY_TOOLS_SETTING, PortalId, "false"));
            var autoWriteTools = bool.Parse(PortalController.GetPortalSetting(AUTO_WRITE_TOOLS_SETTING, PortalId, "false"));
            var debugEnabled = bool.Parse(PortalController.GetPortalSetting(DEBUG_SETTING, PortalId, "false"));

            TornadoToolsService toolsService = CreateToolService();
            List<Tool> tools = null;
            if (request.Mode == "readonly")
            {
                tools = toolsService.GetReadOnlyTools().Where(t => enabledToolNames.Contains(t.ResolvedName)).ToList();
            }
            else if (request.Mode == "agent")
            {
                tools = toolsService.GetAllTools().Where(t => enabledToolNames.Contains(t.ResolvedName)).ToList();
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

            response.Content = new PushStreamContent(async (stream, httpContent, transportContext) =>
            {
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        await WriteSseEvent(writer, "error", new { message = "ApiKey missing (goto settings)" });
                        return;
                    }

                    try
                    {
                        var historyManager = new ChatHistoryManager(conversationsFolder);
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

                        var tornadoService = new TornadoChatService(apiKey, Logger, model, maxTokens);
                        Conversation conv;

                        if (request.RunTool && !string.IsNullOrEmpty(request.ToolName))
                        {
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

                            ReplaceToolPlaceholder(conv, toolCallId,
                                request.ToolApproved
                                    ? await toolsService.ExecuteToolAsync(request.ToolName, request.ToolArguments)
                                    : "The user refused this action.",
                                request.ToolApproved);

                            var toolResultContent = GetToolResultFromConv(conv, toolCallId);
                            chatHistory.AddMessage(new Satrabel.AIChat.History.ChatMessage
                            {
                                Role = MessageRole.Tool,
                                ToolName = request.ToolName,
                                ToolArguments = request.ToolArguments,
                                ToolCallId = toolCallId,
                                Content = toolResultContent
                            });
                            chatHistory.AddMessage(MessageRole.User, $"[Tool Result for {request.ToolName}]: {toolResultContent}");

                            var remaining = request.PendingToolCalls;
                            if (remaining != null && remaining.Count > 0)
                            {
                                SavePendingConversation(chatHistory.Id, conv);
                                historyManager.SaveConversation(chatHistory.Id);

                                var nextTool = remaining[0];
                                var restOfQueue = remaining.Count > 1 ? remaining.Skip(1).ToList() : null;

                                var queueMessages = chatHistory.Messages.Select(m => new TornadoMessageDto
                                {
                                    Role = m.Role.ToString().ToLowerInvariant(),
                                    Content = m.Content,
                                    ToolName = m.ToolName,
                                    ToolArguments = m.ToolArguments,
                                    ToolCallId = m.ToolCallId
                                }).ToList();

                                await WriteSseEvent(writer, "done", new TornadoChatResponse
                                {
                                    Success = true,
                                    Message = string.Empty,
                                    ConversationId = chatHistory.Id,
                                    Messages = queueMessages,
                                    ToolCall = nextTool,
                                    PendingToolCalls = restOfQueue,
                                    TotalInputTokens = 0,
                                    TotalOutputTokens = 0,
                                    TotalPrice = 0
                                });
                                return;
                            }

                            if (!request.ToolApproved)
                            {
                                historyManager.SaveConversation(chatHistory.Id);
                                var refusedMessages = chatHistory.Messages.Select(m => new TornadoMessageDto
                                {
                                    Role = m.Role.ToString().ToLowerInvariant(),
                                    Content = m.Content,
                                    ToolName = m.ToolName,
                                    ToolArguments = m.ToolArguments,
                                    ToolCallId = m.ToolCallId
                                }).ToList();
                                await WriteSseEvent(writer, "done", new TornadoChatResponse
                                {
                                    Success = true,
                                    Message = string.Empty,
                                    ConversationId = chatHistory.Id,
                                    Messages = refusedMessages,
                                    ToolCall = null,
                                    TotalInputTokens = 0,
                                    TotalOutputTokens = 0,
                                    TotalPrice = 0
                                });
                                return;
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
                        List<TornadoToolCallDto> remainingPendingToolCalls = null;
                        const int maxIterations = 100;

                        for (int iteration = 0; iteration < maxIterations; iteration++)
                        {
                            var assistantText = new StringBuilder();
                            List<FunctionCall> capturedFunctionCalls = null;

                            try
                            {
                                await conv.StreamResponseRich(new ChatStreamEventHandler
                                {
                                    MessageTokenHandler = (token) =>
                                    {
                                        assistantText.Append(token);
                                        return new ValueTask(WriteSseEvent(writer, "delta", new { text = token }));
                                    },
                                    FunctionCallHandler = (calls) =>
                                    {
                                        capturedFunctionCalls = calls;
                                        foreach (var call in calls)
                                        {
                                            call.Result = new FunctionResult(call.Name, "resolving...");
                                        }
                                        return default(ValueTask);
                                    }
                                });
                            }
                            catch (Exception apiEx) when (apiEx.Message != null && apiEx.Message.Contains("rate_limit"))
                            {
                                const int waitSeconds = 30;
                                Logger.Warn($"Rate limit hit on iteration {iteration}, waiting {waitSeconds}s: {apiEx.Message}");
                                await WriteSseEvent(writer, "wait", new
                                {
                                    seconds = waitSeconds,
                                    message = $"Rate limit exceeded. Retrying in {waitSeconds} seconds..."
                                });
                                await Task.Delay(waitSeconds * 1000);
                                continue;
                            }

                            var usage = conv.MostRecentApiResult?.Usage;
                            totalInputTokens += usage?.PromptTokens ?? 0;
                            totalOutputTokens += usage?.CompletionTokens ?? 0;

                            if (capturedFunctionCalls != null && capturedFunctionCalls.Count > 0)
                            {
                                var assistantTextStr = assistantText.ToString();
                                if (!string.IsNullOrWhiteSpace(assistantTextStr))
                                {
                                    chatHistory.AddMessage(MessageRole.Assistant, assistantTextStr);
                                }

                                var autoExecute = new List<FunctionCall>();
                                var needsApproval = new List<FunctionCall>();

                                foreach (var call in capturedFunctionCalls)
                                {
                                    bool shouldAuto = (autoReadonlyTools && toolsService.IsReadOnly(call.Name))
                                                   || (autoWriteTools && !toolsService.IsReadOnly(call.Name));
                                    if (shouldAuto)
                                        autoExecute.Add(call);
                                    else
                                        needsApproval.Add(call);
                                }

                                if (autoExecute.Count > 0)
                                {
                                    foreach (var call in autoExecute)
                                    {
                                        await WriteSseEvent(writer, "tool_start", new { toolName = call.Name, arguments = call.Arguments });
                                    }

                                    var autoTasks = autoExecute.Select(async call =>
                                    {
                                        var args = ParseToolArguments(call.Arguments);
                                        var result = await toolsService.ExecuteToolAsync(call.Name, args);
                                        return new { Call = call, Args = args, Result = result };
                                    }).ToArray();

                                    var autoResults = await Task.WhenAll(autoTasks);

                                    foreach (var ar in autoResults)
                                    {
                                        var toolCallId = ar.Call.ToolCall?.Id ?? ar.Call.Name;
                                        ReplaceToolPlaceholder(conv, toolCallId, ar.Result, true);
                                        chatHistory.AddMessage(new Satrabel.AIChat.History.ChatMessage
                                        {
                                            Role = MessageRole.Tool,
                                            ToolName = ar.Call.Name,
                                            ToolArguments = ar.Args,
                                            ToolCallId = toolCallId,
                                            Content = ar.Result
                                        });
                                        chatHistory.AddMessage(MessageRole.User,
                                            $"[Tool Result for {ar.Call.Name}]: {ar.Result}");

                                        await WriteSseEvent(writer, "tool_auto", new { toolName = ar.Call.Name, result = ar.Result });
                                    }
                                }

                                if (needsApproval.Count > 0)
                                {
                                    foreach (var call in needsApproval)
                                    {
                                        var toolCallId = call.ToolCall?.Id ?? call.Name;
                                        ReplaceToolPlaceholder(conv, toolCallId, "Pending user approval.", false);
                                    }

                                    var first = needsApproval[0];
                                    pendingToolCall = new TornadoToolCallDto
                                    {
                                        Id = first.ToolCall?.Id ?? first.Name,
                                        Name = first.Name,
                                        Arguments = ParseToolArguments(first.Arguments),
                                        ReadOnly = toolsService.IsReadOnly(first.Name),
                                        Description = first.Arguments
                                    };

                                    if (needsApproval.Count > 1)
                                    {
                                        remainingPendingToolCalls = needsApproval.Skip(1).Select(call => new TornadoToolCallDto
                                        {
                                            Id = call.ToolCall?.Id ?? call.Name,
                                            Name = call.Name,
                                            Arguments = ParseToolArguments(call.Arguments),
                                            ReadOnly = toolsService.IsReadOnly(call.Name),
                                            Description = call.Arguments
                                        }).ToList();
                                    }

                                    SavePendingConversation(chatHistory.Id, conv);
                                    break;
                                }

                                continue;
                            }
                            else
                            {
                                var text = assistantText.ToString();
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

                        IEnumerable<DebugMessageDto> debugMessages = null;
                        if (debugEnabled && conv?.Messages != null)
                        {
                            debugMessages = conv.Messages.Select(m => new DebugMessageDto
                            {
                                Role = m.Role.ToString().ToLowerInvariant(),
                                Content = m.Content,
                                ToolCallId = m.ToolCallId,
                                ToolCalls = m.ToolCalls?.Select(t => t.Id).ToList(),
                                MessageTokens = m.GetMessageTokens(),
                            }).ToList();
                        }

                        await WriteSseEvent(writer, "done", new TornadoChatResponse
                        {
                            Success = true,
                            Message = string.Empty,
                            ConversationId = chatHistory.Id,
                            Messages = messages,
                            ToolCall = pendingToolCall,
                            PendingToolCalls = remainingPendingToolCalls,
                            TotalInputTokens = totalInputTokens,
                            TotalOutputTokens = totalOutputTokens,
                            TotalPrice = CalculatePrice(model, totalInputTokens, totalOutputTokens),
                            DebugMessages = debugMessages
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error in AITornadoController.ChatStream", ex);
                        await WriteSseEvent(writer, "error", new { message = $"Error: {ex.Message}" });
                    }
                }
            }, new MediaTypeHeaderValue("text/event-stream"));

            return response;
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

        /// <summary>
        /// Replaces a "Pending user approval." placeholder tool_result in the conversation
        /// with the real result. Finds the tool message by matching <paramref name="toolCallId"/>
        /// against the ToolCallId on each Tool-role message.
        /// </summary>
        private static void ReplaceToolPlaceholder(Conversation conv, string toolCallId, string content, bool success)
        {
            if (conv?.Messages == null) return;

            for (int i = conv.Messages.Count - 1; i >= 0; i--)
            {
                var msg = conv.Messages[i];
                if (msg.Role == ChatMessageRoles.Tool && msg.ToolCallId == toolCallId)
                {
                    conv.EditMessageContent(msg, content);
                    msg.ToolInvocationSucceeded = success;
                    return;
                }
            }

            conv.AddToolMessage(toolCallId, content, success);
        }

        /// <summary>
        /// Reads back the tool result content for a given tool call id from the conversation.
        /// </summary>
        private static string GetToolResultFromConv(Conversation conv, string toolCallId)
        {
            if (conv?.Messages == null) return string.Empty;
            for (int i = conv.Messages.Count - 1; i >= 0; i--)
            {
                var msg = conv.Messages[i];
                if (msg.Role == ChatMessageRoles.Tool && msg.ToolCallId == toolCallId)
                    return msg.Content ?? string.Empty;
            }
            return string.Empty;
        }

        private static async Task WriteSseEvent(StreamWriter writer, string eventType, object data)
        {
            await writer.WriteAsync($"event: {eventType}\ndata: {JsonConvert.SerializeObject(data)}\n\n");
            await writer.FlushAsync();
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
                var popularModelOrder = new[]
                {
                    ChatModel.Anthropic.Claude45.Haiku251001.Name,
                    ChatModel.Anthropic.Claude46.Opus.Name,
                    ChatModel.Anthropic.Claude46.Sonnet.Name,
                    ChatModel.Google.Gemini.GeminiFlashLatest.Name,
                    ChatModel.Google.Gemini.GeminiProLatest.Name,
                    ChatModel.OpenAi.Gpt52.V52.Name,
                    ChatModel.OpenAi.Gpt52.V52Pro.Name,
                    ChatModel.OpenAi.Gpt5.V5Mini.Name,
                    ChatModel.Zai.Glm.Glm5.Name,
                    ChatModel.Zai.Glm.Glm47Flash.Name,
                };

                var popularityLookup = popularModelOrder
                    .Select((name, index) => new { name, index })
                    .ToDictionary(x => x.name, x => x.index);

                res.Models = ChatModel.AllModels
                    .Select(m => new ModelDto
                    {
                        Value = m.Name,
                        Name = $"{m.Provider.ToString()} - {m.Name}"
                    })
                    .OrderBy(m =>
                    {
                        return popularityLookup.TryGetValue(m.Value, out var rank)
                            ? rank
                            : int.MaxValue;
                    })
                    .ThenBy(m => m.Name)
                    .ToList();
                res.ApiKey = string.IsNullOrEmpty(apiKey) ? "" : "********";
                res.MaxTokens = int.Parse(PortalController.GetPortalSetting(MAX_TOKENS_SETTING, PortalId, MAX_TOKENS_DEFAULT.ToString()));
                res.HistoryMaxTokens = int.Parse(PortalController.GetPortalSetting(HISTORY_MAX_TOKENS_SETTING, PortalId, ChatHistoryPolicy.DefaultMaxContextTokens.ToString()));
                res.HistoryMaxTurns = int.Parse(PortalController.GetPortalSetting(HISTORY_MAX_TURNS_SETTING, PortalId, "20"));
                res.Model = PortalController.GetPortalSetting(MODEL_SETTING, PortalId, ChatModel.Anthropic.Claude4.Sonnet250514);
                res.AutoReadonlyTools = bool.Parse(PortalController.GetPortalSetting(AUTO_READONLY_TOOLS_SETTING, PortalId, "false"));
                res.AutoWriteTools = bool.Parse(PortalController.GetPortalSetting(AUTO_WRITE_TOOLS_SETTING, PortalId, "false"));
                res.Debug = bool.Parse(PortalController.GetPortalSetting(DEBUG_SETTING, PortalId, "false"));
                var tools = PortalController.GetPortalSetting(TOOLS_SETTING, PortalId, "").Split(',').ToList();
                var toolsService = CreateToolService();
                res.Tools = toolsService.GetAllTools().Select(t => new ToolDto
                {
                    Name = t.ResolvedName,
                    Description = t.ResolvedDescription,
                    Category = toolsService.GetToolDefinition(t.ResolvedName)?.Category,
                    Active = tools.Contains(t.ResolvedName),
                }).OrderBy(t=> t.Name).ToList();

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
            PortalController.UpdatePortalSetting(PortalId, DEBUG_SETTING, request.Debug.ToString());
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

        [ValidateAntiForgeryToken]
        [HttpPost]
        public void SaveChatPreferences(ChatPreferencesDto request)
        {
            if (request == null) return;

            SaveChatPreferences(request.SelectedMode, request.SelectedRule);
        }

        private void SaveChatPreferences(string mode, string rule)
        {
            var validModes = new[] { "chat", "readonly", "agent" };
            if (string.IsNullOrEmpty(mode) || !validModes.Contains(mode))
                mode = "readonly";

            var selectedMode = PortalController.GetPortalSetting(SELECTED_MODE_SETTING, PortalId, "readonly");
            var selectedRule = PortalController.GetPortalSetting(SELECTED_RULE_SETTING, PortalId, "");
            if (mode != selectedMode)
                PortalController.UpdatePortalSetting(PortalId, SELECTED_MODE_SETTING, mode);
            if (rule != selectedRule)
                PortalController.UpdatePortalSetting(PortalId, SELECTED_RULE_SETTING, rule ?? "");
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
            res.Debug = bool.Parse(PortalController.GetPortalSetting(DEBUG_SETTING, PortalId, "false"));
            res.SelectedMode = PortalController.GetPortalSetting(SELECTED_MODE_SETTING, PortalId, "readonly");
            res.SelectedRule = PortalController.GetPortalSetting(SELECTED_RULE_SETTING, PortalId, "");
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
