using System.IO;
using System.Security.Cryptography;

namespace ExodusLauncher.Services;

public static class HashUtil
{
    /// <summary>Streaming SHA-256 of a file, returned as lowercase hex. Built into .NET — no NuGet.</summary>
    public static async Task<string> Sha256FileAsync(string path, CancellationToken ct = default)
    {
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1 << 20, useAsync: true);
        using var sha = SHA256.Create();
        var buffer = new byte[1 << 20];
        int read;
        while ((read = await fs.ReadAsync(buffer, ct)) > 0)
            sha.TransformBlock(buffer, 0, read, null, 0);
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }
}
