using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Lemon.Book.Shell.Infrastructures.Localization;

namespace Lemon.Inside.Book.Controls.Scene;

/// <summary>Labels for the pictures: a localized string drawn at a point, anchored by fractions of its size.</summary>
public static class DiagramText
{
    public static readonly IBrush Ink = new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xF2));
    public static readonly IBrush Faint = new SolidColorBrush(Color.FromArgb(0xB0, 0xE6, 0xEA, 0xF2));
    public static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C));
    public static readonly IBrush Cyan = new SolidColorBrush(Color.FromRgb(0x6F, 0xD8, 0xFF));

    /// <summary>The string with this key in the reader's language.</summary>
    public static string Get(string key) => LocalizationService.Instance.GetString(key);

    public static string Format(string key, params object[] args) => LocalizationService.Instance.Format(key, args);

    /// <summary>
    /// Draws <paramref name="text"/> with its anchor (<paramref name="ax"/>, <paramref name="ay"/>: 0 = left/top,
    /// 0.5 = centre, 1 = right/bottom) at <paramref name="at"/>. Returns the box it took.
    /// </summary>
    public static Rect Draw(DrawingContext context, string text, Point at, double size, IBrush brush,
        double ax = 0, double ay = 0, bool bold = false)
    {
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, bold ? FontWeight.SemiBold : FontWeight.Normal);
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, size, brush);
        var origin = new Point(at.X - (formatted.Width * ax), at.Y - (formatted.Height * ay));
        context.DrawText(formatted, origin);
        return new Rect(origin, new Size(formatted.Width, formatted.Height));
    }
}
