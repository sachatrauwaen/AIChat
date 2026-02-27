using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using DotNetNuke.Web.Api;
using DotNetNuke.Entities.Portals;
using Satrabel.PersonaBar.DnnMcp.Apis;

namespace Dnn.Mcp.WebApi.Middleware
{
    /// <summary>
    /// Authorization attribute for API key validation.
    /// </summary>
    public class ApiKeyAuthorizeAttribute : AuthorizeAttributeBase, IOverrideDefaultAuthLevel
    {
       
        /// <summary>
        /// Extracts the API key from the Authorization header.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <returns>The API key, or null if not found.</returns>
        private string? GetApiKeyFromHeader(AuthFilterContext actionContext)
        {
            if (!actionContext.ActionContext.Request.Headers.Contains("Authorization"))
            {
                return null;
            }

            var authHeader = actionContext.ActionContext.Request.Headers.GetValues("Authorization").FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                return null;
            }

            // Expected format: "Bearer <api-key>"
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            return null;
        }

        public override bool IsAuthorized(AuthFilterContext context)
        {
            if (context == null)
            {
                return false;
            }

            var apiKey = GetApiKeyFromHeader(context);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            var portalSettings = PortalSettings.Current;
            var apiKeySetting = PortalController.GetPortalSetting(DnnMcpController.APIKEY_SETTING, portalSettings.PortalId, "");
            if (apiKeySetting != null && apiKeySetting == apiKey)
            {
                // Check if the API key valid delay is active
                var validDelayActiveStr = PortalController.GetPortalSetting(DnnMcpController.APIKEY_ACTIVE_SETTING, portalSettings.PortalId, "true");
                bool validDelayActive = bool.Parse(validDelayActiveStr);
                
                if (!validDelayActive)
                {
                    return true;
                }
                // Check if the API key has expired
                var validUntilDateStr = PortalController.GetPortalSetting(DnnMcpController.APIKEY_VALID_UNTIL_DATE_SETTING, portalSettings.PortalId, "");
                if (!string.IsNullOrEmpty(validUntilDateStr))
                {
                    if (DateTime.TryParse(validUntilDateStr, out DateTime validUntilDate))
                    {
                        if (DateTime.Now > validUntilDate)
                        {
                            // API key has expired
                            return false;
                        }
                    }
                }
            
                return true;
            }

            return false;
        }
    }
}
