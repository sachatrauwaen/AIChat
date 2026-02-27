using System;
using System.Collections.Generic;
using System.Net.Http;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using Newtonsoft.Json;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class GetHtmlTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-url-html",
                Title = "Get HTML of an URL",
                Description = "Retrieves the HTML content from a specified URL",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "url",
                        Description = "The URL to retrieve HTML content from",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    var url = arguments["url"].ToString();
                    var result = GetHtml(url);

                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock
                            {
                                Text = result
                            }
                        }
                    };
                }
            });
        }

        public string GetHtml(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetStringAsync(url).Result;
                    return response;
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }
    }
} 
