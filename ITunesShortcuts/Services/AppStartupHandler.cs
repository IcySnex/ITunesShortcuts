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
        SystemTray systemTray)
    {
        try
        {
            windowHelper.SetIcon("icon.ico");
            windowHelper.SetSize(480, 800);

            systemTray.Enable();

            mainView.Closed += async (s, e) =>
            {
                systemTray.Disable();

                windowHelper.LoggerView?.Close();

                string config = converter.ToString(configuration.Value);
                await File.WriteAllTextAsync("Configuration.json", config);

                logger.LogInformation("[MainView-Closed] Closed main window");
            };
            mainView.Activate();


            logger.LogInformation("[AppStartupHandler-.ctor] App fully started.");
        }
        catch (Exception ex)
        {
            logger.LogInformation("[AppStartupHandler-.ctor] App failed to start: {error}", ex.Message);

            App.Current.Exit();
        }
    }
}