using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

using AnthropicClient.Models;
using System.Reflection;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    class GetUrlHtmlTool : ITool
    {
        public string Name => "Get HTML of an url";

        public string Description => "Retrieves the HTML content from a specified URL";

        public MethodInfo Function => typeof(GetUrlHtmlTool).GetMethod(nameof(GetUrlHtml));

        public static string GetUrlHtml(string url)
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