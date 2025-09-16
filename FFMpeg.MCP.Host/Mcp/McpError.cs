namespace FFMpeg.MCP.Host.Mcp
{
    public class McpError
    {
        public string Code { get; set; } = "internal_error";
        public string Message { get; set; } = "An unexpected error occurred.";
        public object? Data { get; set; }
    }
}
