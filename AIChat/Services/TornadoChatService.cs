using System.Collections.Generic;
using DotNetNuke.Instrumentation;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;

namespace Satrabel.AIChat.Services
{
    internal class TornadoChatService
    {
        private readonly TornadoApi _api;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly ILog _logger;

        public TornadoChatService(string apiKey, ILog logger, string model, int maxTokens)
        {
            var provider = ChatModel.GetProvider(model);
            _api = new TornadoApi(apiKey, provider.Value);
            _logger = logger;
            _model = model;
            _maxTokens = maxTokens;
        }

        public Conversation CreateConversation(string systemPrompt, List<Tool> tools = null)
        {
            var conversation = _api.Chat.CreateConversation(new ChatRequest
            {
                Model = new ChatModel(_model),
                MaxTokens = _maxTokens,
                Temperature = 0.8,
                Tools = tools,
                ParallelToolCalls = false,
                //InvokeClrToolsAutomatically = false,
                ToolChoice = OutboundToolChoice.Auto
                
            });

            conversation.AppendSystemMessage(systemPrompt);
            return conversation;
        }
    }
}
