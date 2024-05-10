using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ITunesShortcuts.Converters;

public class StringToBitmapImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language) =>
         new BitmapImage(new((string)value)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}