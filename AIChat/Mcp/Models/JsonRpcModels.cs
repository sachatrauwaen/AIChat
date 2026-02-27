using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dnn.Mcp.WebApi.Models.Mcp
{
    /// <summary>
    /// JSON-RPC 2.0 Request object.
    /// </summary>
    public class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public object Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Params { get; set; }

        /// <summary>
        /// Returns true if this is a notification (no id) or a response (no method).
        /// A proper JSON-RPC request has both id and method.
        /// </summary>
        [JsonIgnore]
        public bool IsRequest => Id != null && !string.IsNullOrEmpty(Method);

        [JsonIgnore]
        public bool IsNotification => Id == null && !string.IsNullOrEmpty(Method);
    }

    /// <summary>
    /// JSON-RPC 2.0 Response object.
    /// </summary>
    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcError Error { get; set; }

        public static JsonRpcResponse Success(object id, object result)
        {
            return new JsonRpcResponse { Id = id, Result = result };
        }

        public static JsonRpcResponse ErrorResponse(object id, int code, string message)
        {
            return new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError { Code = code, Message = message }
            };
        }
    }

    /// <summary>
    /// JSON-RPC 2.0 Error object.
    /// </summary>
    public class JsonRpcError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }
    }

    public static class JsonRpcErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
    }
}
