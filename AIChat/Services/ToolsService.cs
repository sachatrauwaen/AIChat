using AnthropicClient.Models;
using DotNetNuke.Instrumentation;
using Satrabel.AIChat.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satrabel.AIChat.Services
{
    internal class ToolsService
    {
        private List<Tool> readOnlyTools = new[] {
                    Tool.CreateFromClass<GetModulesTool>( ),
                    Tool.CreateFromClass<GetModuleTool>( ),
                    Tool.CreateFromClass<GetPagesTool>(),
                    Tool.CreateFromClass<GetHtmlTool>(),
                    Tool.CreateFromClass<GetFoldersTool>(),
                    Tool.CreateFromClass<GetFilesTool>(),
                    Tool.CreateFromClass<ReadFileTool>(new EphemeralCacheControl()),
                    }.ToList();


        private List<Tool> writeTools = new[] {
                    Tool.CreateFromClass<SetModuleTool>(),
                    Tool.CreateFromClass<SendEmailTool>(),
                    Tool.CreateFromClass<WriteFileTool>(),
                    Tool.CreateFromClass<AddPageTool>(),
                    Tool.CreateFromClass<UpdatePageTool>(),
                    Tool.CreateFromClass<AddModuleTool>(),
                    Tool.CreateFromClass<DeletePageTool>(new EphemeralCacheControl()),
                    }.ToList();

        private List<Tool> allTools = new List<Tool>();

        private ILog Logger;

        public ToolsService(ILog logger)
        {
            allTools.AddRange(readOnlyTools);
            allTools.AddRange(writeTools);
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

        public async Task<string> InvokeAsync(ToolCall toolCall) {
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

    }
}
