using Microsoft.Extensions.Logging;

namespace ITunesShortcuts.ViewModels;

public class HomeViewModel
{
    readonly ILogger<HomeViewModel> logger;

    public HomeViewModel(
        ILogger<HomeViewModel> logger)
    {
        this.logger = logger;

        logger.LogInformation("[HomeViewModel-.ctor] HomeViewModel has been initialized.");
    }
}