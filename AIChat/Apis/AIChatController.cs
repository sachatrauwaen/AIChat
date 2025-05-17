using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DotNetNuke.Web.Api;
using Dnn.PersonaBar.Library;
using Dnn.PersonaBar.Library.Attributes;
using Newtonsoft.Json;
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


namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class Constants
    {
        public const string MenuName = "Dnn.AIChat";
        public const string Edit = "EDIT";
    }

    [MenuPermission(MenuName = Constants.MenuName)]
    public class AIChatController : PersonaBarApiController
    {
        private readonly string _apiKey = "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"; // API key from env/config

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ChatResponse> Chat(ChatRequest request)
        {
            throw new NotImplementedException("Chat endpoint is not implemented yet.");
            /*
            var client = new Anthropic.AnthropicClient(_apiKey);
            var response = await client.Messages.MessagesPostAsync(
                model: ModelVariant2.Claude37SonnetLatest,
                messages: new[] {
                    new InputMessage { Role= InputMessageRole.User, Content= "What's the weather like today?" }
                },
                maxTokens: 300,
                metadata: null,
                stopSequences: null,
                system: null,
                temperature: 0,
                toolChoice: null,
                tools: null,
                topK: 0,
                topP: 0,
                stream: false);

            var allMessages = request.Messages;
            allMessages.Add(new MessageDto { Role = "assistant", Content = response.AsSimpleText() });

            return new ChatResponse
            {
                Response = response.AsSimpleText(),
                Messages = allMessages,
                Success = true,
                IsMarkdown = true
            };
            */
            //var client = new AnthropicClient(_apiKey);

            //// Use default markdown formatting
            //var messages = CreateMessagesWithMarkdownFormatting(request.Messages);

            //var text = await client.SendMessageAsync(messages);
            //return new ChatResponse
            //{
            //    Response = text,
            //    Success = true,
            //    IsMarkdown = true
            //};
        }

        /// <summary>
        /// Chat endpoint that uses tools to perform calculations.
        /// </summary>
        /// <remarks>
        /// This endpoint demonstrates the use of tools with Claude AI.
        /// It provides a calculator_add tool that can add two numbers.
        /// 
        /// Example request messages that will trigger tool use:
        /// - "What is 25 plus 37?"
        /// - "Calculate the sum of 123.45 and 67.89"
        /// - "If I have 42 apples and get 18 more, how many do I have in total?"
        /// 
        /// The AI will recognize these as addition tasks and use the calculator_add tool.
        /// </remarks>
        /// <param name="request">Chat request containing the user message</param>
        /// <returns>Response with the AI's message after tool execution</returns>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ChatResponse> ChatWithTools(ChatToolRequest request)
        {
            try
            {
                var client = new AnthropicApiClient(_apiKey, new HttpClient());

                var getWeatherTool = Tool.CreateFromClass<GetWeatherTool>();

                //List<Message> messages = new Message[] {
                //  new Message(
                //    MessageRole.User,
                //    (new Content[] {new TextContent("What is the weather in New York?") }).ToList()
                //  )
                //}.ToList();

                var dtos = request.Messages;
                List<Message> messages = request.Messages.Select(m => new Message(
                  m.Role,
                  new List<Content> { new TextContent(m.Content) }
                )).ToList();

                List<Tool> tools = new[] {
                    Tool.CreateFromClass<GetModulesTool>(),
                    Tool.CreateFromClass<GetModuleTool>(),
                    Tool.CreateFromClass<SetModuleTool>(),
                    Tool.CreateFromClass<GetPagesTool>(),
                    Tool.CreateFromClass<GetHtmlTool>(),
                    Tool.CreateFromClass<SendEmailTool>()
                    }.ToList();

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

                var response = await client.CreateMessageAsync(new MessageRequest(
                  AnthropicModels.Claude35Sonnet,
                  messages,
                  system: $"I am your dnn assistant and can help you with different tasks related to website admin. {hostContext} {portalcontext} {airules}",
                  tools: tools
                ));

                if (!response.IsSuccess)
                {
                    Console.WriteLine("Failed to create message");
                    Console.WriteLine("Error Type: {0}", response.Error.Error.Type);
                    Console.WriteLine("Error Message: {0}", response.Error.Error.Message);
                }

                messages.Add(new Message(MessageRole.Assistant, response.Value.Content));
                dtos.Add(new MessageDto
                {
                    Role = MessageRole.Assistant,
                    Content = string.Join("\n", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.ToText())),
                    ContentType = string.Join(",", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.Type))
                }
                );

                foreach (var content in response.Value.Content)
                {
                    switch (content)
                    {
                        case TextContent textContent:
                            Console.WriteLine(textContent.Text);
                            break;
                        case ToolUseContent toolUseContent:
                            Console.WriteLine(toolUseContent.Name);
                            break;
                    }
                }
                int toolCallCount = 0;
                while (response.Value.ToolCall != null && toolCallCount <= 5)
                {
                    toolCallCount++;

                    var tool = response.Value.ToolCall.ToolUse.ToText();

                    var toolCallResult = await response.Value.ToolCall.InvokeAsync<string>();
                    string toolResultContent;

                    if (toolCallResult.IsSuccess && toolCallResult.Value != null)
                    {
                        Console.WriteLine(toolCallResult.Value);
                        toolResultContent = toolCallResult.Value;
                    }
                    else
                    {
                        Console.WriteLine(toolCallResult.Error.Message);
                        toolResultContent = toolCallResult.Error.Message;
                    }

                    messages.Add(
                          new AnthropicClient.Models.Message(
                            MessageRole.User,
                            new List<Content> {
                              new ToolResultContent(
                                  response.Value.ToolCall.ToolUse.Id,
                                  toolResultContent
                                )}
                          )
                        );

                    dtos.Add(new MessageDto
                    {
                        Role = MessageRole.User,
                        Content = toolResultContent,
                        ContentType = "tool_result",
                        ToolName = response.Value.ToolCall.Tool.Name,
                        ToolFullname = tool
                    });

                    response = await client.CreateMessageAsync(new MessageRequest(
                      AnthropicModels.Claude35Sonnet,
                      messages,
                      tools: tools
                    ));

                    if (!response.IsSuccess)
                    {
                        Console.WriteLine("Failed to create message");
                        Console.WriteLine("Error Type: {0}", response.Error.Error.Type);
                        Console.WriteLine("Error Message: {0}", response.Error.Error.Message);
                    }

                    foreach (var content in response.Value.Content)
                    {
                        switch (content)
                        {
                            case TextContent textContent:
                                Console.WriteLine(textContent.Text);
                                break;
                        }
                    }

                    messages.Add(new Message(MessageRole.Assistant, response.Value.Content));
                    dtos.Add(new MessageDto
                    {
                        Role = MessageRole.Assistant,
                        Content = string.Join("\n", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.ToText())),
                        ContentType = string.Join(",", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.Type))
                    });
                }

                return new ChatResponse
                {
                    //Response = finalResponse, 
                    Messages = dtos,
                    Success = true,
                    Message = "Tool execution completed successfully",
                    // AMessages = messages,
                    // AResponse = response.Value
                };

                /*
                var client = new AnthropicClient(_apiKey);

                // Use default markdown formatting including the user message
                var messages = CreateMessagesWithMarkdownFormatting(request.Messages);

                // Define a calculator tool for addition
                var tools = new List<Tool>
                {
                    new Tool
                    {
                        name = "calculator_add",
                        description = "Adds two numbers together precisely. This tool performs basic addition of two numerical values and returns their exact sum. Use this tool when the user asks to add numbers, sum values, or perform addition calculations. The tool accepts any valid numerical inputs (integers or decimals) and returns the precise mathematical sum. It should not be used for other mathematical operations like subtraction, multiplication, or division.",
                        input_schema = new ToolInput
                        {
                            type = "object",
                            properties = new Dictionary<string, ToolProperty>
                            {
                                ["a"] = new ToolProperty { type = "number", description = "First number to add. Can be any valid integer or decimal value." },
                                ["b"] = new ToolProperty { type = "number", description = "Second number to add. Can be any valid integer or decimal value." }
                            },
                            required = new List<string> { "a", "b" }
                        }
                    }
                };

                // Create a tool executor function
                async Task<object> ToolExecutor(ToolUse toolUse)
                {
                    if (toolUse.name == "calculator_add")
                    {
                        try
                        {
                            Console.WriteLine($"Executing calculator_add with input: {JsonConvert.SerializeObject(toolUse.Input)}");

                            // Parse the input parameters
                            var input = toolUse.GetInput<CalculatorAddInput>();

                            // Validate inputs
                            if (input == null)
                            {
                                return new { error = "Input parameters could not be parsed", is_error = true };
                            }

                            decimal result = input.a + input.b;
                            Console.WriteLine($"Calculator result: {input.a} + {input.b} = {result}");

                            // Return the result as a string to match the documentation example
                            return result.ToString();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in calculator_add: {ex.Message}");
                            // Return the error message as a string with is_error flag
                            return $"Error performing calculation: {ex.Message}";
                        }
                    }

                    return $"Unknown tool: {toolUse.name}";
                }

                try
                {
                    // Get tool choice settings
                    string toolChoice = request.ToolChoice ?? "auto";
                    bool disableParallelToolUse = request.DisableParallelToolUse ?? false;

                    // Execute the conversation with tools
                    var response = await client.ExecuteToolsConversationAsync(
                        messages,
                        tools,
                        ToolExecutor,
                        toolChoice,
                        disableParallelToolUse
                    );

                    // Get the final text response
                    // string finalResponse = response.GetTextContent();

                    return new ChatResponse
                    {
                        //Response = finalResponse, 
                        Messages = response.Messages.Select(m => new MessageDto()
                        {
                            Role = m.Role,
                            Content = m.ContextText
                        }),
                        Success = true,
                        Message = "Tool execution completed successfully",
                        IsMarkdown = true
                    };
                }
                catch (Exception ex)
                {
                    return new ChatResponse
                    {
                        Success = false,
                        Response = null,
                        Message = $"Error during tool execution: {ex.Message}"
                    };
                }
                */
            }
            catch (Exception ex)
            {
                return new ChatResponse
                {
                    Success = false,
                    Response = null,
                    Message = $"Error setting up tools: {ex.Message}"
                };
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ChatResponse> ChatWithTools2(ChatToolRequest request)
        {
            ToolResponse toolResponse = null;
            try
            {
                var client = new AnthropicApiClient(_apiKey, new HttpClient());

                var dtos = request.Messages;

                if (!request.RunTool && dtos.Any() && dtos.Last().AContent?.Any(c => c.Type == "tool_use") == true)
                {
                    dtos.RemoveAt(dtos.Count - 1);
                }

                List<Message> messages = request.Messages.Select(m => new Message(
                  m.Role,
                  GetContent(m)
                )).ToList();

                List<Tool> readOnlyTools = new[] {
                    Tool.CreateFromClass<GetModulesTool>(),
                    Tool.CreateFromClass<GetModuleTool>(),
                    Tool.CreateFromClass<GetPagesTool>(),
                    Tool.CreateFromClass<GetHtmlTool>(),
                    Tool.CreateFromClass<GetFoldersTool>(),
                    Tool.CreateFromClass<GetFilesTool>(),
                    Tool.CreateFromClass<ReadFileTool>(),
                    }.ToList();


                List<Tool> allTools = new[] {
                    Tool.CreateFromClass<GetModulesTool>(),
                    Tool.CreateFromClass<GetModuleTool>(),
                    Tool.CreateFromClass<SetModuleTool>(),
                    Tool.CreateFromClass<GetPagesTool>(),
                    Tool.CreateFromClass<GetHtmlTool>(),
                    Tool.CreateFromClass<SendEmailTool>(),
                    Tool.CreateFromClass<GetFoldersTool>(),
                    Tool.CreateFromClass<GetFilesTool>(),
                    Tool.CreateFromClass<ReadFileTool>(),
                    Tool.CreateFromClass<WriteFileTool>()
                    }.ToList();

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
                var systemPrompt = $"I am your dnn assistant and can help you with different tasks related to website admin. {hostContext} {portalcontext} {airules}";
                AnthropicResult<MessageResponse> response;
                if (request.RunTool)
                {
                    var toolCall = GetToolCall(request.ToolUse, allTools);
                    var tool = toolCall.ToolUse.ToText();

                    var toolCallResult = await toolCall.InvokeAsync<string>();
                    string toolResultContent;

                    if (toolCallResult.IsSuccess && toolCallResult.Value != null)
                    {
                        Console.WriteLine(toolCallResult.Value);
                        toolResultContent = toolCallResult.Value;
                    }
                    else
                    {
                        Console.WriteLine(toolCallResult.Error.Message);
                        toolResultContent = toolCallResult.Error.Message;
                    }

                    messages.Add(
                          new AnthropicClient.Models.Message(
                            MessageRole.User,
                            new List<Content> {
                              new ToolResultContent(
                                  toolCall.ToolUse.Id,
                                  toolResultContent
                                )}
                          )
                        );

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
                    

                    response = await client.CreateMessageAsync(new MessageRequest(
                      AnthropicModels.Claude35Sonnet,
                      messages,
                      tools: request.IsReadOnly ? readOnlyTools : allTools,
                      system: systemPrompt
                    ));

                    if (!response.IsSuccess)
                    {
                        Console.WriteLine("Failed to create message");
                        Console.WriteLine("Error Type: {0}", response.Error.Error.Type);
                        Console.WriteLine("Error Message: {0}", response.Error.Error.Message);
                        throw new Exception($"Failed to create message: {response.Error.Error.Message}");
                    }

                    foreach (var content in response.Value.Content)
                    {
                        switch (content)
                        {
                            case TextContent textContent:
                                Console.WriteLine(textContent.Text);
                                break;
                        }
                    }

                    messages.Add(new Message(MessageRole.Assistant, response.Value.Content));
                    dtos.Add(new MessageDto
                    {
                        Role = MessageRole.Assistant,
                        Content = string.Join("\n", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.ToText())),
                        ContentType = string.Join(",", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.Type)),
                        AContent = response.Value.Content.Select(c => GetContentDto(c)).ToList()
                    });
                }
                else
                {
                    response = await client.CreateMessageAsync(new MessageRequest(
                     AnthropicModels.Claude35Sonnet,
                     messages,
                     system: systemPrompt,
                     tools: request.IsReadOnly ? readOnlyTools : allTools
                   ));

                    if (!response.IsSuccess)
                    {
                        Console.WriteLine("Failed to create message");
                        Console.WriteLine("Error Type: {0}", response.Error.Error.Type);
                        Console.WriteLine("Error Message: {0}", response.Error.Error.Message);
                        throw new Exception($"Failed to create message: {response.Error.Error.Message}");
                    }

                    messages.Add(new Message(MessageRole.Assistant, response.Value.Content));
                    dtos.Add(new MessageDto
                    {
                        Role = MessageRole.Assistant,
                        Content = string.Join("\n", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.ToText())),
                        ContentType = string.Join(",", response.Value.Content.Where(c => c.Type != "tool_use").Select(c => c.Type)),
                        AContent = response.Value.Content.Select(c => GetContentDto(c)).ToList()
                    }
                    );

                    foreach (var content in response.Value.Content)
                    {
                        switch (content)
                        {
                            case TextContent textContent:
                                Console.WriteLine(textContent.Text);
                                break;
                            case ToolUseContent toolUseContent:
                                Console.WriteLine(toolUseContent.Name);
                                break;
                        }
                    }
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



                return new ChatResponse
                {
                    //Response = finalResponse, 
                    Messages = dtos,
                    Success = true,
                    Message = "",
                    //AMessages = messages,
                    //AResponse = response.Value,
                    Tool = toolResponse
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

        private ToolCall GetToolCall(MessageResponse response, List<Tool> tools)
        {
            var toolUse = response.Content.OfType<ToolUseContent>().FirstOrDefault();

            if (toolUse is null)
            {
                return null;
            }

            var tool = tools.FirstOrDefault(t => t.Name == toolUse.Name);

            if (tool is null)
            {
                return null;
            }

            return new ToolCall(tool, toolUse);
        }

        private ToolCall GetToolCall(ToolUseContent toolUse, List<Tool> tools)
        {
            if (toolUse is null)
            {
                return null;
            }

            var tool = tools.FirstOrDefault(t => t.Name == toolUse.Name);

            if (tool is null)
            {
                return null;
            }

            return new ToolCall(tool, toolUse);
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


        public class ChatToolRequest : ChatRequest
        {
            [JsonProperty("toolChoice")]
            public string ToolChoice { get; set; }

            [JsonProperty("disableParallelToolUse")]
            public bool? DisableParallelToolUse { get; set; }


            [JsonProperty("runTool")]
            public bool RunTool { get; set; }

            [JsonProperty("toolUse")]
            public ToolUseContent ToolUse { get; set; }

            [JsonProperty("isReadOnly")]
            public bool IsReadOnly { get; set; }
        }
    }

    public class CalculatorAddInput
    {
        public decimal a { get; set; }
        public decimal b { get; set; }
    }

    public class ChatRequest
    {
        [JsonProperty("messages")]
        public List<MessageDto> Messages { get; set; }

        [JsonProperty("aMessages")]
        public List<MessageDto> AMessages { get; set; }

        [JsonProperty("aResponse")]
        public MessageResponse AResponse { get; set; }

    }

    public class ChatResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("messages")]
        public IEnumerable<MessageDto> Messages { get; set; }

        //[JsonProperty("aMessages")]
        //public List<Message> AMessages { get; set; }

        //[JsonProperty("aResponse")]
        //public MessageResponse AResponse { get; set; }

        [JsonProperty("tool")]
        public ToolResponse Tool { get; set; }
    }

    public class MarkdownPreferences
    {
        [JsonProperty("useHeaders")]
        public bool UseHeaders { get; set; } = true;

        [JsonProperty("useBulletPoints")]
        public bool UseBulletPoints { get; set; } = true;

        [JsonProperty("useCodeBlocks")]
        public bool UseCodeBlocks { get; set; } = true;

        [JsonProperty("useTables")]
        public bool UseTables { get; set; } = true;

        [JsonProperty("useEmphasis")]
        public bool UseEmphasis { get; set; } = true;
    }

    public class MarkdownChatRequest : ChatRequest
    {
        [JsonProperty("markdownPreferences")]
        public MarkdownPreferences MarkdownPreferences { get; set; }
    }
}