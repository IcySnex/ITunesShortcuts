using CommunityToolkit.Mvvm.ComponentModel;
using ITunesShortcuts.Enums;
using Microsoft.Extensions.Logging;

namespace ITunesShortcuts.ViewModels;

public partial class CreateShortcutViewModel : ObservableObject
{
    readonly ILogger<CreateShortcutViewModel> logger;

    public CreateShortcutViewModel(
        ILogger<CreateShortcutViewModel> logger)
    {
        this.logger = logger;

        logger.LogInformation("[CreateShortcutViewModel-.ctor] CreateShortcutViewModel has been initialized.");
    }


    public bool IsValid =>
        !string.IsNullOrWhiteSpace(ShortcutName) &&
        SelectedAction != 0 &&
        SelectedParameter != 0 &&
        Key is not null;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    string shortcutName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    int selectedAction = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    int selectedParameter = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    Key? key = null;

    [ObservableProperty]
    Modifiers modifiers = Modifiers.None;


    [ObservableProperty]
    bool isListeningKey = false;

    partial void OnIsListeningKeyChanged(
        bool value)
    {
        if (!value)
            return;

        logger.LogInformation("[CreateShortcutViewModel-OnIsListeningKeyChanged] Listening to key requested.");

        IsListeningKey = false;
    }
}