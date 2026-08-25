namespace AudioConverter.Core.Progress;

public static class OperationTiming
{
    public static TimeSpan? EstimateRemaining(TimeSpan elapsed, double completedFraction)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }
        if (!double.IsFinite(completedFraction) || completedFraction <= 0)
        {
            return null;
        }
        if (completedFraction >= 1)
        {
            return TimeSpan.Zero;
        }

        var remainingTicks = elapsed.Ticks * ((1 - completedFraction) / completedFraction);
        return TimeSpan.FromTicks((long)Math.Min(remainingTicks, TimeSpan.MaxValue.Ticks));
    }

    public static string FormatCompact(TimeSpan duration)
    {
        var totalSeconds = Math.Max(0, (int)Math.Ceiling(duration.TotalSeconds));
        var days = totalSeconds / 86_400;
        var hours = totalSeconds / 3_600;
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        if (days > 0) return $"{days}d {hours % 24:00}h";
        if (hours > 0) return $"{hours}h {minutes % 60:00}m";
        if (minutes > 0) return $"{minutes}m {seconds:00}s";
        return $"{seconds}s";
    }
}
