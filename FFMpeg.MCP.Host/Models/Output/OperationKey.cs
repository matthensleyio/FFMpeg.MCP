using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;

namespace FFMpeg.MCP.Host.Models.Output;

public class OperationKey
{
    public string FilePath { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public object? Options { get; set; }

    public string GenerateHash()
    {
        var keyObject = new
        {
            FilePath = Path.GetFullPath(FilePath).ToLowerInvariant(),
            OperationType,
            Options
        };

        var json = JsonSerializer.Serialize(keyObject, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
