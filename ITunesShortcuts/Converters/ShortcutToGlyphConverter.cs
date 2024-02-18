using ITunesShortcuts.Enums;
using Microsoft.UI.Xaml.Data;

namespace ITunesShortcuts.Converters;

public class ShortcutToGlyphConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language) =>
         value is ShortcutAction shortcutAction ? shortcutAction switch
         {
             ShortcutAction.None => "\xf142",
             ShortcutAction.Get => "\xe8d6",
             ShortcutAction.Rate => "\xe734",
             ShortcutAction.AddToPlaylist => "\xe8f4",
             ShortcutAction.ViewLyrics => "\xe7bc",
             _ => "\xf142"
         } : "\xf142";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}