using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PersonalRagnarokTool.Infrastructure;

public sealed class BoolToStatusBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBrush = new(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81));   // #10B981 green
    private static readonly SolidColorBrush InactiveBrush = new(System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44)); // #EF4444 red

    static BoolToStatusBrushConverter()
    {
        ActiveBrush.Freeze();
        InactiveBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? ActiveBrush : InactiveBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
