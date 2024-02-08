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
        SystemTray systemTray)
    {
        try
        {
            systemTray.Enable();

            windowHelper.SetIcon("icon.ico");
            windowHelper.SetMinSize(375, 600);
            windowHelper.SetSize(480, 800);

            mainView.Closed += async (s, e) =>
            {
                systemTray.Disable();

                windowHelper.LoggerView?.Close();

                string config = converter.ToString(configuration.Value);
                await File.WriteAllTextAsync("Configuration.json", config);

                logger.LogInformation("[MainView-Closed] Closed main window");
            };
            mainView.Activate();

            navigation.Navigate("Home");

            logger.LogInformation("[AppStartupHandler-.ctor] App fully started.");
        }
        catch (Exception ex)
        {
            logger.LogInformation("[AppStartupHandler-.ctor] App failed to start: {error}", ex.Message);

            App.Current.Exit();
        }
    }
}