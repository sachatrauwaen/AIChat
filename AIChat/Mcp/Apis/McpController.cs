using Dnn.Mcp.WebApi.Middleware;
using Dnn.Mcp.WebApi.Models.Mcp;
using Dnn.Mcp.WebApi.Services.Mcp;
using DotNetNuke.Web.Api;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dnn.Mcp.WebApi.Controllers.Mcp
{
    [ApiKeyAuthorize]
    public class McpController : DnnApiController
    {
        private readonly IMcpHandler handler;

        public McpController(IMcpHandler handler) => this.handler = handler;

        [HttpPost]
        public HttpResponseMessage Post(JsonRpcRequest request) =>
            this.Request.CreateResponse(HttpStatusCode.OK, this.handler.HandleRequest(request));
    }
}