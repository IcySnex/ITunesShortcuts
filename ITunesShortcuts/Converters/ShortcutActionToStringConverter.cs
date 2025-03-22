using ITunesShortcuts.Enums;
using Microsoft.UI.Xaml.Data;

namespace ITunesShortcuts.Converters;

public class ShortcutActionToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language) =>
         value is ShortcutAction shortcutAction ? shortcutAction switch
         {
             ShortcutAction.None => "None",
             ShortcutAction.Get => "Get",
             ShortcutAction.Rate => "Rate",
             ShortcutAction.AddToPlaylist => "Add to Playlist",
             ShortcutAction.ViewLyrics => "View Lyrics",
             ShortcutAction.ShowTrackSummary => "Show Track Summary",
             _ => "N/A"
         } : "N/A";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}