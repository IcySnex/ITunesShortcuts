using ITunesShortcuts.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace ITunesShortcuts.Services;

public class Navigation
{
    readonly ILogger<Navigation> logger;
    readonly Frame contentFrame;

    public Navigation(
        ILogger<Navigation> logger,
        MainView mainView)
    {
        this.logger = logger;
        this.contentFrame = mainView.ContentFrame;

        logger.LogInformation("[Navigation-.ctor] Navigation has been initialized.");
    }


    public void Navigate(
        string page,
        object? parameter = null)
    {
        Type? pageType = Type.GetType($"ITunesShortcuts.Views.{page}View, ITunesShortcuts");
        if (pageType is null)
        {
            logger.LogError("[Navigation-Navigate] Failed to navigate: Could not find page of type '{page}'", page);
            return;
        }

        contentFrame.Navigate(pageType, parameter);
    }

    public void GoBack()
    {
        if (!contentFrame.CanGoBack)
        {
            logger.LogError("[Navigation-GoBack] Failed to go back: Not possible at the time");
            return;
        }

        contentFrame.GoBack();
    }
    public void GoForward()
    {
        if (!contentFrame.CanGoForward)
        {
            logger.LogError("[Navigation-GoForward] Failed to go forward: Not possible at the time'");
            return;
        }
        contentFrame.GoForward();
    }
}