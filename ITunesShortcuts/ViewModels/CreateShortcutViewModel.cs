using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Enums;
using ITunesShortcuts.EventArgs;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace ITunesShortcuts.ViewModels;

public partial class CreateShortcutViewModel : ObservableObject
{
    readonly ILogger<CreateShortcutViewModel> logger;
    readonly KeyboardListener keyboardListener;

    public CreateShortcutViewModel(
        ILogger<CreateShortcutViewModel> logger,
        KeyboardListener keyboardListener)
    {
        this.logger = logger;
        this.keyboardListener = keyboardListener;

        logger.LogInformation("[CreateShortcutViewModel-.ctor] CreateShortcutViewModel has been initialized.");
    }


    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        SelectedAction != 0 &&
        Key is not null &&
        !keyboardListener.GetKeys().Any(key => key.Key == Key);


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    string name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    int selectedAction = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    int selectedParameter = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    Key? key = null;

    public HashSet<Modifier> Modifiers { get; } = new();


    [ObservableProperty]
    bool isListeningKey = false;

    partial void OnIsListeningKeyChanged(
        bool value)
    {
        if (!value)
        {
            logger.LogInformation("[CreateShortcutViewModel-OnIsListeningKeyChanged] Key recording stop requested.");

            keyboardListener.KeyPressed -= OnKeyPressed;
            keyboardListener.BlockKeys = false;
            keyboardListener.ListenForAllKeys = false;
            return;
        }

        logger.LogInformation("[CreateShortcutViewModel-OnIsListeningKeyChanged] Key recording requested.");

        keyboardListener.BlockKeys = true;
        keyboardListener.ListenForAllKeys = true;

        async void OnKeyPressed(object? sender, KeyPressedEventArgs args)
        {
            if (args.Key.IsModifier())
                return;

            Key = args.Key;
            logger.LogInformation("[CreateShortcutViewModel-OnKeyPressed] Key recorded: {key}.", args.Key);

            await Task.Delay(500);
            IsListeningKey = false;
        };
        keyboardListener.KeyPressed += OnKeyPressed;
    }


    [RelayCommand]
    void SetModifier(
        Modifier modifier)
    {
        if (Modifiers.Contains(modifier))
        {
            Modifiers.Remove(modifier);
            return;
        }

        Modifiers.Add(modifier);
    }
}