using System.Globalization;
using System.Windows.Data;
using AudioConverter.Core.Validation;

namespace AudioConverter.Desktop.Converters;

public sealed class TimestampValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double seconds ? TimestampParser.Format(seconds) : "00:00:00.000";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try { return TimestampParser.Parse(value?.ToString() ?? ""); }
        catch { return Binding.DoNothing; }
    }
}
