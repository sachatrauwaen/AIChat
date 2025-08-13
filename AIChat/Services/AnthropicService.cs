using AnthropicClient;
using AnthropicClient.Models;
using DotNetNuke.Instrumentation;
using Satrabel.PersonaBar.AIChat.Apis;
using Satrabel.PersonaBar.AIChat.Apis.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Satrabel.AIChat.Services
{
    internal class AnthropicService
    {
        AnthropicApiClient client;
        ILog Logger;
        string model;
        int maxTokens;
        public AnthropicService(string apiKey, ILog logger, string model, int maxTokens)
        {
            client = new AnthropicApiClient(apiKey, new HttpClient());
            Logger = logger;
            this.model = model;
            this.maxTokens = maxTokens;
        }

        public async Task<AnthropicResult<MessageResponse>> CreateMessageAsync(string system, List<MessageDto> messages, List<Tool> tools)
        {

            var response = await client.CreateMessageAsync(new MessageRequest(
                      model,
                      GetMessageList(messages),
                      maxTokens: maxTokens,
                      temperature: 0.1m,
                      tools: tools,
                      // system: system,
                      systemMessages: new List<TextContent> { 
                          new TextContent("Output always in markdown format. When using tools, please use them one at a time and wait for results before making additional tool calls."),
                          new TextContent(system, new EphemeralCacheControl()) }
                    ));

            if (!response.IsSuccess)
            {
                Logger.Error("Failed to create message");
                Logger.ErrorFormat("Error Type: {0}", response.Error.Error.Type);
                Logger.ErrorFormat("Error Message: {0}", response.Error.Error.Message);
                throw new Exception($"Failed to create message: {response.Error.Error.Message}");
            }
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
            return response;
        }

        public List<Message> GetMessageList(List<MessageDto> messages)
        {
            return messages.Select(m => new Message(
                  m.Role,
                  GetContent(m)
                )).ToList();
        }

        private static List<Content> GetContent(MessageDto m)
        {
            var res = new List<Content>();
            if (m.AContent != null)
            {
                foreach (var item in m.AContent)
                {
                    if (item.Type == "text")
                    {
                        res.Add(new TextContent(item.Text));
                    }
                    else if (item.Type == "tool_use")
                    {
                        var toolUse = new ToolUseContent()
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Input = item.Input

                        };
                        res.Add(toolUse);
                    }
                    else if (item.Type == "tool_result")
                    {
                        var toolResult = new ToolResultContent(item.Id, item.Result);
                        res.Add(toolResult);
                    }
                }
            }
            else
            {
                return new List<Content> { new TextContent(m.Content) };
            }
            return res;
        }

        public async Task<List<ModelDto>> GetModelsAsync()
        {
            var res = new List<ModelDto>();
            var response = await client.ListModelsAsync(new PagingRequest(beforeId: AnthropicModels.Claude3Haiku));
            if (!response.IsSuccess)
            {
                Logger.Error("Failed to create message");
                Logger.ErrorFormat("Error Type: {0}", response.Error.Error.Type);
                Logger.ErrorFormat("Error Message: {0}", response.Error.Error.Message);
                throw new Exception($"Failed to create message: {response.Error.Error.Message}");
            }
            //foreach (var model in models)
            {
                res.AddRange(response.Value.Data.Select(m => new ModelDto()
                {
                    Name = m.DisplayName,
                    Value = m.Id,
                }));
            }
            return res;
        }
    }
}
