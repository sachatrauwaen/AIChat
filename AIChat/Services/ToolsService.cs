using AnthropicClient.Models;
using DotNetNuke.Framework.Reflections;
using DotNetNuke.Instrumentation;
using Satrabel.AIChat.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Satrabel.AIChat.Services
{
    internal class ToolsService
    {
        private List<Tool> readOnlyTools = new[] {
                    Tool.CreateFromClass<GetModulesTool>( ),
                    Tool.CreateFromClass<GetHTMLModuleTool>( ),
                    //Tool.CreateFromClass<GetPagesTool>(),
                    //ToolCreate(new GetPagesTool()),                    
                    Tool.CreateFromClass<GetHtmlTool>(),
                    Tool.CreateFromClass<GetSystemFilesTool>(),
                    Tool.CreateFromClass<ReadSystemFileTool>(),
                    Tool.CreateFromClass<GetFoldersTool>(),
                    Tool.CreateFromClass<GetFilesTool>(),
                    Tool.CreateFromClass<ReadFileTool>(new EphemeralCacheControl()),
                    }.ToList();

        private static Tool ToolCreate(IAIChatTool tool)
        {
            return Tool.CreateFromInstanceMethod(tool.Name, tool.Description, tool, tool.Function.Name);
        }

        private List<Tool> writeTools = new[] {
                    Tool.CreateFromClass<SetHTMLModuleTool>(),
                    Tool.CreateFromClass<SendEmailTool>(),
                    Tool.CreateFromClass<WriteFileTool>(),
                    Tool.CreateFromClass<WriteSystemFileTool>(),
                    Tool.CreateFromClass<AddPageTool>(),
                    Tool.CreateFromClass<UpdatePageTool>(),
                    Tool.CreateFromClass<AddModuleTool>(),
                    Tool.CreateFromClass<DeletePageTool>(new EphemeralCacheControl()),
                    }.ToList();

        private List<Tool> allTools = new List<Tool>();

        private Dictionary<string,IAIChatTool> customTools = new Dictionary<string, IAIChatTool>();

        private ILog Logger;

        public ToolsService(ILog logger)
        {
            allTools.AddRange(writeTools);
            allTools.AddRange(readOnlyTools);
            var tools = GetTools();
            foreach (var tool in tools)
            {
                try
                {
                    var t = ToolCreate(tool);
                    if (!allTools.Any(t2 => t2.Name == t.Name))
                    {
                        customTools.Add(t.Name, tool);
                        if (tool.ReadOnly)
                        {
                            readOnlyTools.Add(t);
                        }
                        else
                        {
                            writeTools.Add(t);
                        }
                        allTools.Add(t);
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Error creating tool {tool.GetType().FullName}: {e.Message}", e);
                }
            }
            var lastTool = readOnlyTools.LastOrDefault();
            if (lastTool != null)
            {
                lastTool.CacheControl = new EphemeralCacheControl();
            }
            Logger = logger;
        }

        public List<Tool> GetReadOnlyTools()
        {
            return readOnlyTools;
        }

        public List<Tool> GetWriteTools()
        {
            return writeTools;
        }

        public List<Tool> GetAllTools()
        {
            return allTools;
        }

        public ToolCall GetToolCall(ToolUseContent toolUse)
        {
            if (toolUse is null)
            {
                return null;
            }

            var tool = GetAllTools().FirstOrDefault(t => t.Name == toolUse.Name);

            if (tool is null)
            {
                return null;
            }

            return new ToolCall(tool, toolUse);
        }

        public async Task<string> InvokeAsync(ToolCall toolCall)
        {
            var toolCallResult = await toolCall.InvokeAsync<string>();
            string toolResultContent;

            if (toolCallResult.IsSuccess && toolCallResult.Value != null)
            {
                Logger.Info(toolCallResult.Value);
                toolResultContent = toolCallResult.Value;
            }
            else
            {
                Logger.Error(toolCallResult.Error.Message);
                toolResultContent = toolCallResult.Error.Message;
            }
            return toolResultContent;
        }

        private IEnumerable<IAIChatTool> GetTools()
        {
            var typeLocator = new TypeLocator();
            IEnumerable<Type> types = typeLocator.GetAllMatchingTypes(IsValidToolProvider);

            foreach (Type filterType in types)
            {
                IAIChatTool filter;
                try
                {
                    filter = Activator.CreateInstance(filterType) as IAIChatTool;
                }
                catch (Exception e)
                {
                    Logger.Error($"Unable to create {filterType.FullName} while GetTools. {e.Message}");
                    filter = null;
                }

                if (filter != null)
                {
                    yield return filter;
                }
            }
        }

        private static bool IsValidToolProvider(Type t)
        {
            return t != null && t.IsClass && !t.IsAbstract && t.IsVisible && typeof(IAIChatTool).IsAssignableFrom(t);
        }

        public string GetToolFolder(string toolName)
        {
            if (customTools.ContainsKey(toolName))
            {
                return customTools[toolName].RulesFolder;
            }
            return string.Empty;
        }
    }
}
