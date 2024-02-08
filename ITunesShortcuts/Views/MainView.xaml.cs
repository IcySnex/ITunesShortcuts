using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace ITunesShortcuts.Views;

public sealed partial class MainView : Window
{
    public MainView()
    {
        SystemBackdrop = MicaController.IsSupported() ? new MicaBackdrop() : new DesktopAcrylicBackdrop();
        this.InitializeComponent();
    }
}