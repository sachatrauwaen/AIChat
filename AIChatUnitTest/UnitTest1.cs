using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Satrabel.PersonaBar.AIChat.Services;
using System.Linq;

namespace AIChatUnitTest
{
    [TestClass]
    public class AnthropicClientTests
    {
        private const string TestApiKey = "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";

        [TestMethod]
        public async Task SendMessageAsync_ReturnsExpectedResponse()
        {
            // Arrange - Create a real AnthropicClient
            var client = new AnthropicClient(TestApiKey);
            
            var messages = new List<Message>
            {
                new Message("user", "Hello! Please respond with a brief greeting only.")
            };

            // Act - Call the real method with real HTTP request
            var result = await client.SendMessageAsync(messages);

            // Assert - Check that we got a response
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
            Console.WriteLine($"Response: {result}");
        }

        [TestMethod]
        public async Task SendMessageWithToolsAsync_ReturnsToolUseResponse()
        {
            // Arrange - Create a real AnthropicClient
            var client = new AnthropicClient(TestApiKey);
            
            var messages = new List<Message>
            {
                new Message("user", "Search for information about Paris.")
            };
            
            var tools = new List<Tool>
            {
                new Tool
                {
                    name = "search_database",
                    description = "Search the database for information",
                    input_schema = new ToolInput
                    {
                        type = "object",
                        properties = new Dictionary<string, ToolProperty>
                        {
                            { "query", new ToolProperty { type = "string", description = "Search query" } }
                        },
                        required = new List<string> { "query" }
                    }
                }
            };

            // Act - Call the real method with real HTTP request
            var response = await client.SendMessageWithToolsAsync(messages, tools);

            // Assert - Check we got a valid response
            Assert.IsNotNull(response);
            Console.WriteLine($"Response type: {response.type}");
            Console.WriteLine($"Stop reason: {response.stop_reason}");
            
            var textContent = response.GetTextContent();
            Console.WriteLine($"Text content: {textContent}");
            
            // Check for tool use if applicable
            var toolUse = response.GetToolUse();
            if (toolUse != null)
            {
                Console.WriteLine($"Tool use requested: {toolUse.name}");
                Console.WriteLine($"Tool input: {JsonConvert.SerializeObject(toolUse.Input)}");
            }
            else
            {
                Console.WriteLine("No tool use requested");
            }
        }
        
        [TestMethod]
        public async Task ExecuteToolsConversationAsync_CompletesConversationWithTools()
        {
            // Arrange
            var client = new AnthropicClient(TestApiKey);
            
            var messages = new List<Message>
            {
                new Message("user", "What's the current time in New York and Tokyo?")
            };
            
            var tools = new List<Tool>
            {
                new Tool
                {
                    name = "get_current_time",
                    description = "Get the current time in a specified location",
                    input_schema = new ToolInput
                    {
                        type = "object",
                        properties = new Dictionary<string, ToolProperty>
                        {
                            { "location", new ToolProperty { type = "string", description = "Location name (city, country, etc.)" } }
                        },
                        required = new List<string> { "location" }
                    }
                }
            };

            int toolCallCount = 0;
            
            // Tool executor function
            async Task<object> ToolExecutor(ToolUse toolUse)
            {
                toolCallCount++;
                Console.WriteLine($"Tool execution #{toolCallCount}");
                Console.WriteLine($"Tool: {toolUse.name}");
                Console.WriteLine($"Input: {JsonConvert.SerializeObject(toolUse.Input)}");
                
                if (toolUse.name == "get_current_time")
                {
                    // Parse the input to get the location
                    var input = toolUse.GetInput<CurrentTimeInput>();
                    string location = input.location;
                    
                    // In a real implementation, this would check the actual time
                    // For testing, we'll return mock data
                    string time;
                    if (location.Contains("New York"))
                    {
                        time = DateTime.UtcNow.AddHours(-5).ToString("yyyy-MM-dd HH:mm:ss");
                        return new { location = "New York", time = time, timezone = "EST" };
                    }
                    else if (location.Contains("Tokyo"))
                    {
                        time = DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd HH:mm:ss");
                        return new { location = "Tokyo", time = time, timezone = "JST" };
                    }
                    else
                    {
                        return new { error = $"Unknown location: {location}" };
                    }
                }
                
                return new { error = $"Unknown tool: {toolUse.name}" };
            }
            
            // Act
            var res = await client.ExecuteToolsConversationAsync(
                messages, 
                tools, 
                ToolExecutor
            );

            var finalResponse = res.FinalResponse;

            // Assert
            Assert.IsNotNull(finalResponse);
            Console.WriteLine("Final response:");
            Console.WriteLine($"Stop reason: {finalResponse.stop_reason}");
            Console.WriteLine($"Content: {finalResponse.GetTextContent()}");
            Console.WriteLine($"Total tool calls: {toolCallCount}");
            
            // The conversation should have completed
            Assert.IsTrue(toolCallCount > 0, "Tool should have been called at least once");
            Assert.AreEqual("end_turn", finalResponse.stop_reason, "Conversation should have completed with end_turn");
        }
    }

    internal class CurrentTimeInput
    {
        public string location { get; set; }
    }
}
