using ITunesShortcuts.Enums;
using Microsoft.UI.Xaml.Data;

namespace ITunesShortcuts.Converters;

public class ModifiersToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
         value is null ? 0 : (Modifier)value switch
         {
             Modifier.LAlt => 164,
             Modifier.RAlt => 165,
             Modifier.LCtrl => 162,
             Modifier.RCtrl => 163,
             Modifier.LShift => 160,
             Modifier.RShift => 161,
             Modifier.LWin => 91,
             Modifier.RWin => 92,
             _ => 0
         };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is null ? default : (int)value switch
        {
            164 => Modifier.LAlt,
            165 => Modifier.RAlt,
            162 => Modifier.LCtrl,
            163 => Modifier.RCtrl,
            160 => Modifier.LShift,
            161 => Modifier.RShift,
            91 => Modifier.LWin,
            92 => Modifier.RWin,
            _ => default
        };
}