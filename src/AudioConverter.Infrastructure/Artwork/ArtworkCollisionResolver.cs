using System.Security.Cryptography;

namespace AudioConverter.Infrastructure.Artwork;

public sealed record ArtworkCollisionDecision(string OutputPath, bool IsIdentical);

public static class ArtworkCollisionResolver
{
    public static async Task<ArtworkCollisionDecision> ResolveAsync(
        string stagedPath,
        string desiredPath,
        CancellationToken cancellationToken = default)
    {
        var stagedHash = await HashAsync(stagedPath, cancellationToken);
        var directory = Path.GetDirectoryName(desiredPath)!;
        var baseName = Path.GetFileNameWithoutExtension(desiredPath);
        var extension = Path.GetExtension(desiredPath);
        for (var index = 1; index <= 10_000; index++)
        {
            var candidate = index == 1 ? desiredPath : Path.Combine(directory, $"{baseName}_{index}{extension}");
            if (!File.Exists(candidate)) return new ArtworkCollisionDecision(candidate, false);
            var candidateHash = await HashAsync(candidate, cancellationToken);
            if (CryptographicOperations.FixedTimeEquals(stagedHash, candidateHash))
                return new ArtworkCollisionDecision(candidate, true);
        }
        throw new IOException("Too many artwork files share the same output name.");
    }

    private static async Task<byte[]> HashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true);
        return await SHA256.HashDataAsync(stream, cancellationToken);
    }
}
