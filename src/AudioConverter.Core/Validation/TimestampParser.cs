using System.Globalization;

namespace AudioConverter.Core.Validation;

public static class TimestampParser
{
    public static double Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var parts = value.Trim().Replace(',', '.').Split(':');
        if (parts.Length is < 1 or > 3)
        {
            throw Invalid(value);
        }

        var numbers = new double[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!double.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[index]) ||
                !double.IsFinite(numbers[index]) ||
                numbers[index] < 0)
            {
                throw Invalid(value);
            }
        }

        if (numbers.Length == 1)
        {
            return numbers[0];
        }

        if (numbers[^1] >= 60)
        {
            throw Invalid(value);
        }

        if (numbers.Length == 2)
        {
            if (numbers[0] != Math.Truncate(numbers[0]))
            {
                throw Invalid(value);
            }

            return numbers[0] * 60 + numbers[1];
        }

        if (numbers[0] != Math.Truncate(numbers[0]) ||
            numbers[1] != Math.Truncate(numbers[1]) ||
            numbers[1] >= 60)
        {
            throw Invalid(value);
        }

        return numbers[0] * 3600 + numbers[1] * 60 + numbers[2];
    }

    public static string Format(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        var totalMilliseconds = (long)Math.Round(seconds * 1000, MidpointRounding.AwayFromZero);
        var hours = totalMilliseconds / 3_600_000;
        var remainder = totalMilliseconds % 3_600_000;
        var minutes = remainder / 60_000;
        remainder %= 60_000;
        var wholeSeconds = remainder / 1000;
        var milliseconds = remainder % 1000;
        return $"{hours:00}:{minutes:00}:{wholeSeconds:00}.{milliseconds:000}";
    }

    private static FormatException Invalid(string value) =>
        new($"Invalid timestamp: {value}");
}
