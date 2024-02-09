using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Models;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITunesShortcuts.ViewModels;

public partial class SettingsViewModel
{
    readonly ILogger<SettingsViewModel> logger;
    readonly Navigation navigation;

    public Config Configuration { get; }

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IOptions<Config> configuration,
        Navigation navigation)
    {
        this.logger = logger;
        this.navigation = navigation;
        this.Configuration = configuration.Value;

        logger.LogInformation("[SettingsViewModel-.ctor] SettingsViewModel has been initialized.");
    }


    [RelayCommand]
    void GoBack() =>
        navigation.GoBack();
}