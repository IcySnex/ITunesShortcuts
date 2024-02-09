using Microsoft.UI.Xaml.Controls;
using ITunesShortcuts.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ITunesShortcuts.Views;

public sealed partial class SettingsView : Page
{
    readonly SettingsViewModel viewModel = App.Provider.GetRequiredService<SettingsViewModel>();

    public SettingsView()
    {
        this.InitializeComponent();
    }
}