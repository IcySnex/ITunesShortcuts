using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Models;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;

namespace ITunesShortcuts.ViewModels;

public partial class HomeViewModel
{
    readonly ILogger<HomeViewModel> logger;
    readonly Navigation navigation;

    public ITunesInfo? Info { get; }

    public HomeViewModel(
        ILogger<HomeViewModel> logger,
        Navigation navigation,
        ITunesHelper iTunesHelper)
    {
        this.logger = logger;
        this.navigation = navigation;

        Info = iTunesHelper.GetInfo();

        logger.LogInformation("[HomeViewModel-.ctor] HomeViewModel has been initialized.");
    }


    [RelayCommand]
    void NavigateToSettings() =>
        navigation.Navigate("Settings");
}