using ITunesShortcuts.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ITunesShortcuts.Views;

public sealed partial class CreateShortcutView : UserControl
{
    readonly CreateShortcutViewModel viewModel;

    public CreateShortcutView(
        CreateShortcutViewModel viewModel)
    {
        this.viewModel = viewModel;

        this.InitializeComponent();
    }
}