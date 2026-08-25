using System.Text.RegularExpressions;
using AudioConverter.Core.Models;

namespace AudioConverter.Core.Validation;

public static partial class OptionValidator
{
    public static void Validate(ConversionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var lossy = options.OutputFormat.IsLossy();

        if (options.Bitrate is not null && !BitratePattern().IsMatch(options.Bitrate))
            throw new ArgumentException("Bitrate must look like 192k or 1M.");
        if (!lossy && options.Bitrate is not null)
            throw new ArgumentException("Bitrate does not apply to WAV or FLAC.");
        if (lossy && (options.SampleRate is not null || options.BitDepth is not null))
            throw new ArgumentException("Sample rate and bit depth apply only to WAV or FLAC.");
        if (options.SampleRate is not null && options.SampleRate is < 8_000 or > 384_000)
            throw new ArgumentException("Sample rate must be between 8000 and 384000 Hz.");
        if (options.BitDepth is not null && options.BitDepth is not (16 or 24 or 32))
            throw new ArgumentException("Bit depth must be 16, 24, or 32.");
        if (options.OutputFormat == AudioFormat.Flac && options.BitDepth == 32)
            throw new ArgumentException("FLAC output supports 16-bit or 24-bit depth.");
    }

    [GeneratedRegex("^[1-9][0-9]*[kKmM]$")]
    private static partial Regex BitratePattern();
}
