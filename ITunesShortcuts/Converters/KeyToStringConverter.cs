using ITunesShortcuts.Enums;
using Microsoft.UI.Xaml.Data;

namespace ITunesShortcuts.Converters;

public class KeyToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language) =>
         value is Key key ? key.ToString().Replace("Key", "") : null;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}