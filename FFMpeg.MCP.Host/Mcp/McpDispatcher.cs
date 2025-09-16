using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FFMpeg.MCP.Host.Mcp
{
    public class McpDispatcher
    {
        private readonly ILogger<McpDispatcher> _logger;

        public McpDispatcher(ILogger<McpDispatcher> logger)
        {
            _logger = logger;
        }

        public async Task<McpResponse<T>> DispatchAsync<T>(Func<Task<T>> action)
        {
            try
            {
                var result = await action();
                return McpResponse<T>.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP handler failed");
                return new McpResponse<T> { Error = McpErrorMapper.FromException(ex) };
            }
        }
    }
}
