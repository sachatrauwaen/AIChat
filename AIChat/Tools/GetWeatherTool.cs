
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AnthropicClient.Models;
using System.Reflection;

namespace Satrabel.AIChat.Tools
{



    class GetWeatherTool : ITool
    {
        public string Name => "Get Weather";

        public string Description => "Get the weather for a location in the specified units";

        public MethodInfo Function => typeof(GetWeatherTool).GetMethod(nameof(GetWeather));

        public static string GetWeather(string location, string units)
        {
            return $"The weather in {location} is 72 degrees {units}";
        }
    }
}
