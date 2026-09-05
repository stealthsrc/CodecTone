using System.Globalization;
using AudioConverter.Core.Artwork;

namespace AudioConverter.Infrastructure.Artwork;

public static class ArtworkCommandBuilder
{
    public static string[] Build(
        string inputPath,
        string outputPath,
        EmbeddedArtworkInfo artwork,
        ArtworkExtractionOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(artwork);
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaximumDimension is < 64 or > 8192)
            throw new ArgumentException("Maximum artwork dimension must be between 64 and 8192 pixels.");
        if (options.OutputFormat == ArtworkOutputFormat.Original && options.MaximumDimension is not null)
            throw new ArgumentException("Original artwork cannot be resized without selecting a conversion format.");

        var arguments = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-y", "-i", inputPath,
            "-map", $"0:{artwork.StreamIndex}", "-an", "-frames:v", "1",
        };
        if (options.MaximumDimension is { } maximum)
        {
            var number = maximum.ToString(CultureInfo.InvariantCulture);
            arguments.AddRange(["-vf", $"scale=w='min({number},iw)':h='min({number},ih)':force_original_aspect_ratio=decrease"]);
        }

        switch (options.OutputFormat)
        {
            case ArtworkOutputFormat.Original: arguments.AddRange(["-c:v", "copy"]); break;
            case ArtworkOutputFormat.Png: arguments.AddRange(["-c:v", "png"]); break;
            case ArtworkOutputFormat.Jpeg: arguments.AddRange(["-c:v", "mjpeg", "-q:v", "2"]); break;
            case ArtworkOutputFormat.Webp: arguments.AddRange(["-c:v", "libwebp", "-quality", "90"]); break;
            default: throw new ArgumentOutOfRangeException(nameof(options));
        }
        arguments.AddRange(["-progress", "pipe:1", "-nostats", outputPath]);
        return [.. arguments];
    }
}
