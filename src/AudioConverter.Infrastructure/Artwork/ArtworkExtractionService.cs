using AudioConverter.Core.Artwork;
using AudioConverter.Infrastructure.Ffmpeg;
namespace AudioConverter.Infrastructure.Artwork;

public sealed class ArtworkExtractionService(FfmpegTools tools)
{
    private readonly FfmpegProcessRunner runner = new();
    private readonly FfprobeService probe = new(tools.FfprobePath);

    public async Task<ArtworkExtractionResult> ExtractAsync(
        ArtworkPlannedAlbum album, string outputDirectory, ArtworkExtractionOptions options,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        foreach (var candidate in new[] { album }.Concat(album.Fallbacks))
        {
            string? staged = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var extension = options.OutputFormat switch
                {
                    ArtworkOutputFormat.Original => ArtworkPlanner.OriginalExtension(candidate.Artwork.CodecName),
                    ArtworkOutputFormat.Png => "png",
                    ArtworkOutputFormat.Jpeg => "jpg",
                    ArtworkOutputFormat.Webp => "webp",
                    _ => throw new ArgumentOutOfRangeException(nameof(options)),
                };
                Directory.CreateDirectory(outputDirectory);
                var desired = Path.Combine(outputDirectory, album.OutputBaseName + "." + extension);
                staged = Path.Combine(outputDirectory, $".artwork-{Guid.NewGuid():N}.tmp.{extension}");
                await runner.RunAsync(tools.FfmpegPath,
                    ArtworkCommandBuilder.Build(candidate.SourcePath, staged, candidate.Artwork, options),
                    1, progress, cancellationToken);
                _ = await probe.ProbeImageAsync(staged, cancellationToken);
                var collision = await ArtworkCollisionResolver.ResolveAsync(staged, desired, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (collision.IsIdentical) return new ArtworkExtractionResult(candidate, collision.OutputPath, null, true);
                File.Move(staged, collision.OutputPath);
                return new ArtworkExtractionResult(candidate, collision.OutputPath, null, false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) { errors.Add($"{candidate.SourcePath}: {error.Message}"); }
            finally
            {
                if (staged is not null)
                    try { File.Delete(staged); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
        return new ArtworkExtractionResult(album, null, string.Join("\n", errors), false);
    }
}
