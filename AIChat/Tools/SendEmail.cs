using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AnthropicClient.Models;
using System.Reflection;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Mail;
using Newtonsoft.Json;
using DotNetNuke.Entities.Host;
using System.Net.Mail;

namespace Satrabel.AIChat.Tools
{
    class SendEmailTool : ITool
    {
        public string Name => "Send Email";

        public string Description => "Sends an email using DotNetNuke mail services";

        public MethodInfo Function => typeof(SendEmailTool).GetMethod(nameof(SendEmail));

        public static string SendEmail(string toEmail, string subject, string body, bool isHtml = false)
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