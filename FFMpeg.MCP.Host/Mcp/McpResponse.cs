namespace FFMpeg.MCP.Host.Mcp
{
    public class McpResponse<T>
    {
        public T? Result { get; set; }
        public McpError? Error { get; set; }

        public static McpResponse<T> FromResult(T result) =>
            new McpResponse<T> { Result = result };

        public static McpResponse<T> FromError(string code, string message, object? data = null) =>
            new McpResponse<T>
            {
                Error = new McpError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };
    }
}
