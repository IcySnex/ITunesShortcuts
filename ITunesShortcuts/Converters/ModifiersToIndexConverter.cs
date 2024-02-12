using ITunesShortcuts.Enums;
using Microsoft.UI.Xaml.Data;

namespace ITunesShortcuts.Converters;

public class ModifiersToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
         (Modifiers)value switch
         {
             Modifiers.None => 0,
             Modifiers.Alt => 1,
             Modifiers.Ctrl => 2,
             Modifiers.Shift => 3,
             Modifiers.Win => 4,
             _ => -1
         };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        (int)value switch
        {
            0 => Modifiers.None,
            1 => Modifiers.Alt,
            2 => Modifiers.Ctrl,
            3 => Modifiers.Shift,
            4 => Modifiers.Win,
            _ => default
        };
}