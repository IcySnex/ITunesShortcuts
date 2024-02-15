using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Models;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITunesShortcuts.ViewModels;

public partial class SettingsViewModel
{
    readonly ILogger<SettingsViewModel> logger;
    readonly WindowHelper windowHelper;
    readonly Navigation navigation;
    readonly Notifications notifications;

    public Config Configuration { get; }
    public ITunesInfo? Info { get; }

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IOptions<Config> configuration,
        WindowHelper windowHelper,
        Navigation navigation,
        Notifications notifications,
        ITunesHelper iTunesHelper)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;
        this.navigation = navigation;
        this.notifications = notifications;
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


    [RelayCommand]
    void Debug()
    {
        string[] menus = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
        void onSelect(string? item)
        {
            logger.LogInformation(item);
        }

        notifications.SendWithComboBox(menus, onSelect, "pagination with comboBox notifications");
    }
}