using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AudioConverter.Desktop.Services;

public static class TitleBarTheme
{
    private const int UseImmersiveDarkMode = 20;
    private const int BorderColor = 34;
    private const int CaptionColor = 35;
    private const int TextColor = 36;

    public static void Apply(Window window, string theme)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == nint.Zero)
        {
            window.SourceInitialized += (_, _) => Apply(window, theme);
            return;
        }

        var oled = !theme.Equals("White", StringComparison.OrdinalIgnoreCase);
        var immersive = oled ? 1 : 0;
        var caption = ColorRef(oled ? 0x00 : 0xF7, oled ? 0x00 : 0xF7, oled ? 0x00 : 0xF5);
        var text = ColorRef(oled ? 0xF6 : 0x11, oled ? 0xF6 : 0x12, oled ? 0xF6 : 0x15);
        var border = ColorRef(oled ? 0x34 : 0xD7, oled ? 0x34 : 0xD7, oled ? 0x39 : 0xD2);

        _ = DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref immersive, sizeof(int));
        _ = DwmSetWindowAttribute(handle, CaptionColor, ref caption, sizeof(uint));
        _ = DwmSetWindowAttribute(handle, TextColor, ref text, sizeof(uint));
        _ = DwmSetWindowAttribute(handle, BorderColor, ref border, sizeof(uint));
    }

    private static uint ColorRef(int red, int green, int blue) =>
        (uint)(red | green << 8 | blue << 16);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        nint window,
        int attribute,
        ref int value,
        int valueSize);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static extern int DwmSetWindowAttribute(
        nint window,
        int attribute,
        ref uint value,
        int valueSize);
}
