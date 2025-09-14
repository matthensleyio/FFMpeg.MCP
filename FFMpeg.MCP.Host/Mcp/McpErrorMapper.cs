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
                    Code = McpErrorCodes.InvalidArgument,
                    Message = ex.Message
                },
                FileNotFoundException fnf => new McpError
                {
                    Code = McpErrorCodes.NotFound,
                    Message = fnf.Message,
                    Data = new { fnf.FileName }
                },
                DirectoryNotFoundException dnf => new McpError
                {
                    Code = McpErrorCodes.NotFound,
                    Message = dnf.Message,
                },
                UnauthorizedAccessException => new McpError
                {
                    Code = McpErrorCodes.Unauthorized,
                    Message = ex.Message
                },
                _ => new McpError
                {
                    Code = McpErrorCodes.InternalError,
                    Message = ex.Message
                }
            };
        }
    }
}
