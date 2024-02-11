using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ITunesShortcuts.Selectors;

public class LastItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate NormalTemplate { get; set; } = default!;
    public DataTemplate LastItemTemplate { get; set; } = default!;

    protected override DataTemplate SelectTemplateCore(
        object item,
        DependencyObject container)
    {
        ItemsControl? itemsControl = ItemsControl.ItemsControlFromItemContainer(container);
        if (itemsControl is not null && item is not null && itemsControl.Items.IndexOf(item) == itemsControl.Items.Count - 1)
                return LastItemTemplate;

        return NormalTemplate;

    }
}
