using ITunesShortcuts.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
namespace ITunesShortcuts.Views;

public sealed partial class HomeView : Page
{
    readonly HomeViewModel viewModel = App.Provider.GetRequiredService<HomeViewModel>(); 

    public HomeView()
    {
        DataContext = viewModel;

        this.InitializeComponent();
    }
}