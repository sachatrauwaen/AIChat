using Dnn.Mcp.WebApi.Middleware;
using Dnn.Mcp.WebApi.Models.Mcp;
using Dnn.Mcp.WebApi.Services.Mcp;
using DotNetNuke.Instrumentation;
using DotNetNuke.Web.Api;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dnn.Mcp.WebApi.Controllers.Mcp
{
    [ApiKeyAuthorize]
    public class McpController : DnnApiController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(McpController));

        private readonly IMcpHandler handler;

        public McpController(IMcpHandler handler) => this.handler = handler;

        [HttpPost]
        public HttpResponseMessage Post(JsonRpcRequest request)
        {
            // A body that is absent, malformed, or missing "method" deserializes to null or
            // to a request with no method, which would otherwise NRE inside the handler.
            if (request == null || string.IsNullOrEmpty(request.Method))
            {
                return this.Request.CreateResponse(
                    HttpStatusCode.OK,
                    JsonRpcResponse.ErrorResponse(
                        request?.Id, JsonRpcErrorCodes.InvalidRequest, "Invalid JSON-RPC request."));
            }

            try
            {
                var response = this.handler.HandleRequest(request);

                // Notifications carry no id and expect no response body. Serializing the
                // null response instead emits a bare "null" payload, which strict clients
                // reject as a malformed JSON-RPC message.
                if (response == null)
                {
                    return this.Request.CreateResponse(HttpStatusCode.Accepted);
                }

                return this.Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                // JSON-RPC carries application-level failures inside a 200 response. Letting
                // the exception escape produces an HTML 500, which MCP clients treat as a
                // transport failure and abandon the session over.
                Logger.Error("MCP method '" + request.Method + "' failed.", ex);

                return this.Request.CreateResponse(
                    HttpStatusCode.OK,
                    JsonRpcResponse.ErrorResponse(
                        request.Id, JsonRpcErrorCodes.InternalError, ex.Message));
            }
        }
    }
}
