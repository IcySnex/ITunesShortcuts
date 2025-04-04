using iTunesLib;
using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Models;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace ITunesShortcuts.ViewModels;

public partial class SettingsViewModel
{
    readonly WindowHelper windowHelper;
    readonly Navigation navigation;
    readonly DiscordRichPresence discordRichPresence;

    public Config Configuration { get; }
    public ITunesInfo? Info { get; }

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IOptions<Config> configuration,
        WindowHelper windowHelper,
        Navigation navigation,
        ITunesHelper iTunesHelper,
        DiscordRichPresence discordRichPresence)
    {
        this.Configuration = configuration.Value;
        this.windowHelper = windowHelper;
        this.navigation = navigation;
        this.discordRichPresence = discordRichPresence;

        Info = iTunesHelper.GetInfo();

        Configuration.PropertyChanged += OnConfigurationPropertyChanged;

        logger.LogInformation("[SettingsViewModel-.ctor] SettingsViewModel has been initialized.");
    }


    void OnConfigurationPropertyChanged(
        object? _,
        PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Configuration.DiscordRichPresence):
                discordRichPresence.Enabled = Configuration.DiscordRichPresence;
                break;
        }
    }


    [RelayCommand]
    void GoBack() =>
        navigation.GoBack();


    [RelayCommand]
    void Logger() =>
        windowHelper.CreateLoggerView();
}