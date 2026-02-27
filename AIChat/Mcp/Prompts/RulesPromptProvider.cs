using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Models.Mcp;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;

namespace Dnn.Mcp.WebApi.Prompts
{
    public class RulesPromptProvider : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            var rulesPath = PortalSettings.Current.HomeSystemDirectoryMapPath + "mcp/prompts";

            if (Directory.Exists(rulesPath))
            {
                var ruleFiles = Directory.GetFiles(rulesPath, "*.md");

                foreach (var ruleFile in ruleFiles)
                {
                    var ruleName = Path.GetFileNameWithoutExtension(ruleFile);
                    var ruleContent = File.ReadAllText(ruleFile);

                    // Create a prompt for each rule file
                    registry.RegisterPrompt(new PromptDefinition
                    {
                        Name = $"rule_{ruleName}",
                        Title = $"AI Rule: {ruleName}",
                        Description = $"Apply the '{ruleName}' AI rule for specific guidance",
                        Parameters = new List<PromptParameter>(),
                        Handler = (arguments) =>
                        {
                            var promptText = $"Please apply the following AI rule '{ruleName}':\n\n";

                            promptText += ruleContent;

                            var result = new PromptGetResult
                            {
                                Description = $"AI rule: {ruleName}",
                                Messages = new[]
                                {
                                    new PromptMessage
                                    {
                                        Role = "user",
                                        Content = new PromptContent
                                        {
                                            Type = "text",
                                            Text = promptText
                                        }
                                    }
                                }
                            };
                            return result;
                        }
                    });
                }
            }
        }
    }
}
