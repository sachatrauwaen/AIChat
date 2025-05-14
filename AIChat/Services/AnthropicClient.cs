using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Satrabel.PersonaBar.AIChat.Services
{
    public class AnthropicClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://api.anthropic.com/v1/messages";

        public AnthropicClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            
            // Use the latest API version that supports tools
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            //_httpClient.DefaultRequestHeaders.Add("content-type", "application/json");
            
            // Add content-type header
            //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Sends a message to Claude API without tools.
        /// </summary>
        /// <param name="messages">The conversation history.</param>
        /// <returns>The text response from Claude.</returns>
        public async Task<string> SendMessageAsync(List<Message> messages)
        {
            return await SendMessageAsync(messages, null);
        }

        /// <summary>
        /// Sends a message to Claude API with tools, returning the full response including
        /// any tool use requests.
        /// </summary>
        /// <param name="messages">The conversation history.</param>
        /// <param name="tools">The list of tools to make available to Claude.</param>
        /// <param name="toolChoice">How Claude should use tools: "auto" (default), "any", or a specific tool name.</param>
        /// <param name="disableParallelToolUse">Whether to disable Claude's ability to use multiple tools in parallel.</param>
        /// <returns>The full response from Claude, which may include a tool use request.</returns>
        public async Task<AnthropicResponse> SendMessageWithToolsAsync(
            List<Message> messages, 
            List<Tool> tools, 
            string toolChoice = "auto", 
            bool disableParallelToolUse = false)
        {
            // Construct the tool_choice parameter based on the provided toolChoice
            object toolChoiceObj;
            
            if (toolChoice == "auto")
            {
                toolChoiceObj = "auto";
            }
            else if (toolChoice == "any")
            {
                toolChoiceObj = "any";
            }
            else if (!string.IsNullOrEmpty(toolChoice))
            {
                // Specific tool is requested
                toolChoiceObj = new { type = "tool", name = toolChoice };
            }
            else
            {
                toolChoiceObj = "auto"; // Default
            }

            var payload = new
            {
                model = "claude-3-7-sonnet-20250219",
                max_tokens = 1024,
                temperature = 1.0,
                messages,
                tools,
                //tool_choice = toolChoiceObj,
                //disable_parallel_tool_use = disableParallelToolUse
            };

            var jsonPayload = JsonConvert.SerializeObject(payload, Formatting.Indented, 
                new JsonSerializerSettings { 
                    NullValueHandling = NullValueHandling.Ignore 
                });
            Console.WriteLine($"Request payload: {jsonPayload}");

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
            try {
                var response = await _httpClient.PostAsync(ApiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error: {(int)response.StatusCode} {response.StatusCode}");
                    Console.WriteLine($"Response: {responseString}");
                    throw new Exception($"API request failed: {(int)response.StatusCode} {response.StatusCode}\nResponse: {responseString}");
                }
                
                response.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<AnthropicResponse>(responseString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a tool response message for sending back the result of executing a tool.
        /// </summary>
        /// <param name="toolUseId">The ID of the tool use request.</param>
        /// <param name="toolResult">The result from executing the tool.</param>
        /// <param name="isError">Whether the tool execution resulted in an error.</param>
        /// <returns>A Message object representing the tool response.</returns>
        public Message CreateToolResponseMessage(string toolUseId, object toolResult, bool isError = false)
        {
            // Create a tool result content block
            var toolResultBlock = new Dictionary<string, object>
            {
                { "type", "tool_result" },
                { "tool_use_id", toolUseId }
            };

            // Add content based on the type of result
            if (toolResult != null)
            {
                toolResultBlock["content"] = JsonConvert.SerializeObject(toolResult);
            }

            // Add is_error flag if this is an error
            if (isError)
            {
                toolResultBlock["is_error"] = true;
            }

            return new Message
            {
                Role = "user",
                Content = new[] { toolResultBlock },                
            };
        }

        /// <summary>
        /// Creates a tool response message for an error that occurred during tool execution.
        /// </summary>
        /// <param name="toolUseId">The ID of the tool use request.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A Message object representing the error response.</returns>
        public Message CreateToolErrorResponseMessage(string toolUseId, string errorMessage)
        {
            return CreateToolResponseMessage(toolUseId, errorMessage, true);
        }

        /// <summary>
        /// Executes a complete conversation with tools, automatically handling tool execution
        /// via the provided toolExecutor function.
        /// </summary>
        /// <param name="messages">Initial conversation messages.</param>
        /// <param name="tools">Available tools.</param>
        /// <param name="toolExecutor">Function to execute a tool given a tool use request.</param>
        /// <param name="toolChoice">How Claude should use tools: "auto" (default), "any", or a specific tool name.</param>
        /// <param name="disableParallelToolUse">Whether to disable Claude's ability to use multiple tools in parallel.</param>
        /// <returns>The final Claude response after all tool interactions are complete.</returns>
        public async Task<ConversationResponse> ExecuteToolsConversationAsync(
            List<Message> messages, 
            List<Tool> tools, 
            Func<ToolUse, Task<object>> toolExecutor,
            string toolChoice = "auto",
            bool disableParallelToolUse = false)
        {
            var conversation = new List<Message>(messages);
            var res= new ConversationResponse()
            {
                Messages = conversation                
            };
            AnthropicResponse response = null;
            int iterationCount = 0;
            const int MaxIterations = 5; // Prevent infinite loops

            try
            {
                do
                {
                    iterationCount++;
                    Console.WriteLine($"Tool conversation iteration: {iterationCount}");
                    
                    // Log the current conversation state
                    Console.WriteLine($"Current conversation state ({conversation.Count} messages):");
                    foreach (var msg in conversation)
                    {
                        string contentStr = msg.Content?.ToString() ?? "null";
                        if (contentStr.Length > 100) contentStr = contentStr.Substring(0, 100) + "...";
                        Console.WriteLine($"- {msg.Role}: {contentStr}");
                    }

                    response = await SendMessageWithToolsAsync(
                        conversation, 
                        tools, 
                        toolChoice, 
                        disableParallelToolUse
                    );
                    
                    Console.WriteLine($"Response type: {response.type}, stop_reason: {response.stop_reason}");
                    
                    // Use the helper method to extract tool use information
                    var toolUse = response.GetToolUse();
                    
                    if (toolUse != null)
                    {
                        Console.WriteLine($"Tool use requested: {toolUse.name}");
                        
                        // Execute the tool
                        var toolResult = await toolExecutor(toolUse);
                        Console.WriteLine($"Tool execution completed, result: {JsonConvert.SerializeObject(toolResult)}");
                        
                        // Add the assistant's message
                        var assistantMessage = new Message 
                        { 
                            Role = "assistant",
                            Content = response.content,
                            ContextText = response.GetTextContent()
                        };
                        conversation.Add(assistantMessage);
                        Console.WriteLine("Added assistant message with tool request");
                        
                        // Add the tool response
                        var toolResponseMessage = CreateToolResponseMessage(toolUse.id, toolResult);
                        conversation.Add(toolResponseMessage);
                        Console.WriteLine("Added tool response message");
                    }
                    else
                    {
                        // No more tool calls, we're done
                        conversation.Add(new Message
                        {
                            Role = "assistant",
                            Content = response.content,
                            ContextText = response.GetTextContent()
                        });
                        Console.WriteLine("No tool use in response, conversation complete");
                        break;
                    }

                    if (iterationCount >= MaxIterations)
                    {
                        Console.WriteLine($"Reached maximum iteration count ({MaxIterations}), breaking loop");
                        break;
                    }
                }
                while (true);

                res.FinalResponse= response;
                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExecuteToolsConversationAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Sends a message to Claude API with optional tools support.
        /// </summary>
        /// <param name="messages">The conversation history.</param>
        /// <param name="tools">Optional list of tools to make available to Claude.</param>
        /// <returns>The text response from Claude.</returns>
        public async Task<string> SendMessageAsync(List<Message> messages, List<Tool> tools)
        {
            var payload = new
            {
                model = "claude-3-7-sonnet-20250219",
                max_tokens = 1024,
                temperature = 1.0,
                messages,
                tools,
                // tool_choice = tools != null && tools.Count > 0 ? "auto" : null
            };

            var jsonPayload = JsonConvert.SerializeObject(payload, Formatting.Indented, 
                new JsonSerializerSettings { 
                    NullValueHandling = NullValueHandling.Ignore 
                });
            
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(ApiUrl, content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AnthropicResponse>(responseString);
            return result?.GetTextContent() ?? "No response.";
        }
    }

    /// <summary>
    /// Represents a message in a conversation with Claude.
    /// </summary>
    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; }
        
        [JsonProperty("content")]
        public object Content { get; set; } // Can be string or a list of content blocks
        
        [JsonProperty("tool_result")]
        public ToolCallResult tool_result { get; set; }

        [JsonIgnore]
        public string ContextText { get; set; }

        public Message()
        {
        }

        public Message(string role, string content)
        {
            this.Role = role;
            this.Content = content;
        }
    }

    /// <summary>
    /// Represents the result of executing a tool.
    /// </summary>
    public class ToolCallResult
    {
        public string type { get; set; } = "tool_result";
        public string tool_use_id { get; set; }
        public object content { get; set; } // Can be string, object, or error
    }

    /// <summary>
    /// Represents the response from Claude API.
    /// </summary>
    public class AnthropicResponse
    {
        public string id { get; set; }
        public string type { get; set; }
        public string role { get; set; }
        public List<ContentItem> content { get; set; }
        
        public string model { get; set; }
        public string stop_reason { get; set; }
        public string stop_sequence { get; set; }
        public Usage usage { get; set; }
        
        // Helper method to extract text content
        public string GetTextContent()
        {
            return string.Join("\n", content?
                .Where(c => c.type == "text")
                .Select(c => c.text) ?? new List<string>());
        }
        
        // Helper method to extract tool use request
        public ToolUse GetToolUse()
        {
            var toolUseContent = content?.FirstOrDefault(c => c.type == "tool_use");
            if (toolUseContent == null) return null;
            
            return new ToolUse
            {
                id = toolUseContent.id,
                name = toolUseContent.name,
                Input = toolUseContent.input
            };
        }
    }

    /// <summary>
    /// Represents content returned by Claude API.
    /// </summary>
    public class ContentItem
    {
        public string type { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public object input { get; set; }
    }

    /// <summary>
    /// Represents a tool definition that Claude can use.
    /// </summary>
    public class Tool
    {
        public string name { get; set; }
        public string description { get; set; }
        public ToolInput input_schema { get; set; }
    }

    /// <summary>
    /// Represents the input schema for a tool.
    /// </summary>
    public class ToolInput
    {
        public string type { get; set; }
        public Dictionary<string, ToolProperty> properties { get; set; }
        public List<string> required { get; set; }
    }

    /// <summary>
    /// Represents a property definition in a tool's input schema.
    /// </summary>
    public class ToolProperty
    {
        public string type { get; set; }
        public string description { get; set; }
    }

    /// <summary>
    /// Represents a tool use request from Claude.
    /// </summary>
    public class ToolUse
    {
        public string id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        [JsonProperty("input")]
        public object Input { get; set; }

        public T GetInput<T>()
        {
            if (Input is string inputStr)
            {
                return JsonConvert.DeserializeObject<T>(inputStr);
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(Input));
            }
        }
    }

    /// <summary>
    /// Represents token usage information.
    /// </summary>
    public class Usage
    {
        public int input_tokens { get; set; }
        public int output_tokens { get; set; }
    }

    /// <summary>
    /// Represents a content block in a message.
    /// </summary>
    public class ContentBlock
    {
        public string type { get; set; } // "text" or "image"
        public string text { get; set; } // Used when type is "text"
        public ImageSource source { get; set; } // Used when type is "image"
    }

    /// <summary>
    /// Represents an image source in a content block.
    /// </summary>
    public class ImageSource
    {
        public string type { get; set; } // "base64" or "url"
        public string media_type { get; set; } // e.g., "image/jpeg", "image/png"
        public string data { get; set; } // Base64 encoded image data or URL
    }
} 