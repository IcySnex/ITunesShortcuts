using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using ITunesShortcuts.ViewModels;
using ITunesShortcuts.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ITunesShortcuts.Services;

public class AppStartupHandler
{
    readonly ILogger<AppStartupHandler> logger;
    readonly Config configuration;
    readonly WindowHelper windowHelper;
    readonly JsonConverter converter;
    readonly Notifications notifications;
    readonly ITunesHelper iTunesHelper;
    readonly SystemTray systemTray;
    readonly KeyboardListener keyboardListener;

    readonly string artworkLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempArtwork.jpg");

    public AppStartupHandler(
        ILogger<AppStartupHandler> logger,
        IOptions<Config> configuration,
        MainView mainView,
        WindowHelper windowHelper,
        JsonConverter converter,
        Navigation navigation,
        Notifications notifications,
        ITunesHelper iTunesHelper,
        SystemTray systemTray,
        ShortcutManager shortcutManager,
        KeyboardListener keyboardListener)
    {
        this.logger = logger;
        this.configuration = configuration.Value;
        this.windowHelper = windowHelper;
        this.converter = converter;
        this.notifications = notifications;
        this.iTunesHelper = iTunesHelper;
        this.systemTray = systemTray;
        this.keyboardListener = keyboardListener;

        try
        {
            iTunesHelper.ValidateInstallation();
            iTunesHelper.ValidateCOMRegistration();
            iTunesHelper.ValidateInitialization();
            iTunesHelper.OnTrackChanged += (s, e) =>
            {
                if (!configuration.Value.ShowTrackSummary || e.OldTrack is null)
                    return;

                iTunesHelper.SaveArtwork(artworkLocation, e.OldTrack);

                mainView.DispatcherQueue.TryEnqueue(() =>
                {
                    TrackSummaryViewModel viewModel = new(logger, windowHelper, iTunesHelper, e.OldTrack, artworkLocation);
                    windowHelper.CreateTrackSummaryView(viewModel);
                });
            };

            notifications.Register();
            notifications.ClearAsync().AsTask();

            keyboardListener.Start();

            shortcutManager.Load();

            windowHelper.SetIcon("icon.ico");
            windowHelper.SetMinSize(375, 600);
            windowHelper.SetSize(480, 800);

            mainView.Closed += (s, e) =>
            {
                if (configuration.Value.MinimizeToTray)
                {
                    windowHelper.SetVisibility(false);
                    systemTray.ToggleWindowItem.Text = "Show window";

                    e.Handled = true;
                    return;
                }

                PrepareShutdown();
            };

            systemTray.Enable();
            if (configuration.Value.LaunchMinimized)
            {
                windowHelper.SetVisibility(false);
                systemTray.ToggleWindowItem.Text = "Show window";
            }
            else
            {
                mainView.Activate();
            }

            navigation.Navigate("Home");

            logger.LogInformation("[AppStartupHandler-.ctor] App fully started.");
        }
        catch (Exception ex)
        {
            logger.LogError("[AppStartupHandler-.ctor] App failed to start: {error} ({message})", ex.Message, ex.InnerException?.Message ?? "There was an unexcepted error.");
            _ = Win32.MessageBox(IntPtr.Zero, $"Error: {ex.Message}\n{ex.InnerException?.Message ?? "There was an unexcepted error."}", $"Error", Win32.MB_OK | Win32.MB_ICONERROR);

#if DEBUG
            throw;
#else
            Process.GetCurrentProcess().Kill();
#endif
        }
    }


    public void PrepareShutdown()
    {
        notifications.Unregister();

        keyboardListener.Stop();

        systemTray.Disable();

        iTunesHelper.Dispose();

        windowHelper.LoggerView?.Close();
        windowHelper.LyricsView?.Close();
        windowHelper.TrackSummaryView?.Close();

        string config = converter.ToString(configuration);
        File.WriteAllText("configuration.json", config);

        logger.LogInformation("[AppStartupHandler-PrepareShutdown] Closed main window.");
    }
}