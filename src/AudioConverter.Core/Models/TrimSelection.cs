namespace AudioConverter.Core.Models;

public sealed record TrimSelection
{
    private TrimSelection(
        double startSeconds,
        double endSeconds,
        double fadeInSeconds,
        double fadeOutSeconds)
    {
        StartSeconds = startSeconds;
        EndSeconds = endSeconds;
        FadeInSeconds = fadeInSeconds;
        FadeOutSeconds = fadeOutSeconds;
    }

    public double StartSeconds { get; }
    public double EndSeconds { get; }
    public double FadeInSeconds { get; }
    public double FadeOutSeconds { get; }
    public double DurationSeconds => EndSeconds - StartSeconds;

    public static TrimSelection Create(
        double startSeconds,
        double endSeconds,
        double fadeInSeconds = 0,
        double fadeOutSeconds = 0)
    {
        var values = new[] { startSeconds, endSeconds, fadeInSeconds, fadeOutSeconds };
        if (values.Any(value => double.IsNaN(value) || double.IsInfinity(value)))
        {
            throw new ArgumentException("Trim values must be finite numbers.");
        }

        if (startSeconds < 0 || endSeconds <= startSeconds)
        {
            throw new ArgumentException("Trim end must be greater than its non-negative start.");
        }

        if (fadeInSeconds < 0 || fadeOutSeconds < 0)
        {
            throw new ArgumentException("Fade durations cannot be negative.");
        }

        if (fadeInSeconds + fadeOutSeconds > endSeconds - startSeconds)
        {
            throw new ArgumentException("Combined fade durations cannot exceed the selection.");
        }

        return new TrimSelection(startSeconds, endSeconds, fadeInSeconds, fadeOutSeconds);
    }
}
