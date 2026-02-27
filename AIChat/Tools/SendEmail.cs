using System;
using System.Collections.Generic;
using System.Text;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Mail;
using Newtonsoft.Json;
using DotNetNuke.Entities.Host;
using Dnn.Mcp.WebApi;

namespace Satrabel.AIChat.Tools
{
    public class SendEmailTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "send-email",
                Title = "Send Email",
                Description = "Sends an email using DotNetNuke mail services",
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "toEmail",
                        Description = "Email address of email destination",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "subject",
                        Description = "Subject of email",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "body",
                        Description = "Body of email",
                        Required = true,
                        Type = "string"
                    },
                    new ToolParameter
                    {
                        Name = "isHtml",
                        Description = "Is the email body in HTML format",
                        Required = false,
                        Type = "boolean"
                    }
                },
                Handler = (arguments) =>
                {
                    var toEmail = arguments["toEmail"].ToString();
                    var subject = arguments["subject"].ToString();
                    var body = arguments["body"].ToString();
                    var isHtml = arguments.ContainsKey("isHtml") ? Convert.ToBoolean(arguments["isHtml"]) : false;
                    
                    var result = SendEmail(toEmail, subject, body, isHtml);

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

        public string SendEmail(string toEmail, string subject, string body, bool isHtml = false)
        {
            try
            {
                var portalSettings = PortalSettings.Current;
                var fromEmail = Host.HostEmail;
                var fromName = portalSettings.PortalName;
                
                var result = Mail.SendMail(fromEmail,
                                          fromEmail, // From Name
                                          toEmail, 
                                          "", // CC
                                          "", // BCC
                                          "", // Reply To
                                          DotNetNuke.Services.Mail.MailPriority.Normal,
                                          subject,
                                          isHtml ? MailFormat.Html : MailFormat.Text, // Body format
                                          Encoding.UTF8, // Body as byte array
                                          body, 
                                          new List<MailAttachment>(), // Attachment
                                          "", // SMTP Server (use portal default)
                                          "", // SMTP Authentication (use portal default)
                                          "", // SMTP Username (use portal default)
                                          "",
                                          Host.EnableSMTPSSL); // SMTP Password (use portal default)
                
                if (!string.IsNullOrEmpty(result))
                {
                    return JsonConvert.SerializeObject(new { success = false, error = result });
                }
                else
                {
                    return JsonConvert.SerializeObject(new { success = true, message = "Email sent successfully" });
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
} 
