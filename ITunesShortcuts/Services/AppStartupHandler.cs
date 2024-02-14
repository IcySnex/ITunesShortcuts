using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using ITunesShortcuts.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITunesShortcuts.Services;

public class AppStartupHandler
{
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
        try
        {
            iTunesHelper.ValidateInstallation();
            iTunesHelper.ValidateCOMRegistration();

            notifications.Register();
            notifications.ClearAsync().AsTask();

            keyboardListener.Start();

            shortcutManager.Load();

            windowHelper.SetIcon("icon.ico");
            windowHelper.SetMinSize(375, 600);
            windowHelper.SetSize(480, 800);

            mainView.Closed += (s, e) =>
            {
                notifications.Unregister();

                keyboardListener.Stop();

                systemTray.Disable();

                windowHelper.LoggerView?.Close();

                string config = converter.ToString(configuration.Value);
                File.WriteAllText("configuration.json", config);

                logger.LogInformation("[MainView-Closed] Closed main window.");
            };

            systemTray.Enable();
            if (configuration.Value.LaunchMinimized)
                systemTray.ToggleWindow();
            else
                mainView.Activate();

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
}