using System;
using System.IO;

namespace FFMpeg.MCP.Host.Mcp
{
    public static class McpErrorMapper
    {
        public static McpError FromException(Exception ex)
        {
            return ex switch
            {
                ArgumentException => new McpError
                {
                    Code = "invalid_argument",
                    Message = ex.Message
                },
                FileNotFoundException fnf => new McpError
                {
                    Code = "not_found",
                    Message = fnf.Message,
                    Data = new { fnf.FileName }
                },
                DirectoryNotFoundException dnf => new McpError
                {
                    Code = "not_found",
                    Message = dnf.Message,
                },
                UnauthorizedAccessException => new McpError
                {
                    Code = "unauthorized",
                    Message = ex.Message
                },
                _ => new McpError
                {
                    Code = "internal_error",
                    Message = ex.Message
                }
            };
        }
    }
}
