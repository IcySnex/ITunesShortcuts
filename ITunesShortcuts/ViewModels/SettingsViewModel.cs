using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Models;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITunesShortcuts.ViewModels;

public partial class SettingsViewModel
{
    readonly WindowHelper windowHelper;
    readonly Navigation navigation;

    public Config Configuration { get; }
    public ITunesInfo? Info { get; }

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IOptions<Config> configuration,
        WindowHelper windowHelper,
        Navigation navigation,
        ITunesHelper iTunesHelper)
    {
        this.windowHelper = windowHelper;
        this.navigation = navigation;
        this.Configuration = configuration.Value;

        Info = iTunesHelper.GetInfo();

        logger.LogInformation("[SettingsViewModel-.ctor] SettingsViewModel has been initialized.");
    }


    [RelayCommand]
    void GoBack() =>
        navigation.GoBack();


    [RelayCommand]
    void Logger() =>
        windowHelper.CreateLoggerView();
}