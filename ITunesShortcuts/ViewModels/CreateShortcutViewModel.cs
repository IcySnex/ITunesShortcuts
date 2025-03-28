﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Enums;
using ITunesShortcuts.EventArgs;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;

namespace ITunesShortcuts.ViewModels;

public partial class CreateShortcutViewModel : ObservableObject
{
    readonly ILogger<CreateShortcutViewModel> logger;
    readonly ITunesHelper iTunesHelper;
    readonly KeyboardListener keyboardListener;

    public CreateShortcutViewModel(
        ILogger<CreateShortcutViewModel> logger,
        ITunesHelper iTunesHelper,
        KeyboardListener keyboardListener)
    {
        this.logger = logger;
        this.iTunesHelper = iTunesHelper;
        this.keyboardListener = keyboardListener;

        logger.LogInformation("[CreateShortcutViewModel-.ctor] CreateShortcutViewModel has been initialized.");
    }


    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        Action != ShortcutAction.None &&
        Key is not null;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    string name = string.Empty;


    public ShortcutAction[] Actions { get; } = Enum.GetValues<ShortcutAction>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    ShortcutAction action = ShortcutAction.None;

    partial void OnActionChanged(
        ShortcutAction value)
    {
        switch (value)
        {
            case ShortcutAction.None:
            case ShortcutAction.Get:
            case ShortcutAction.ShowTrackSummary:
                Parameters = new[] { "None" };
                break;
            case ShortcutAction.Rate:
                Parameters = new[] { "Select later", "Reset", "1 Star", "2 Stars", "3 Stars", "4 Stars", "5 Stars" };
                break;
            case ShortcutAction.AddToPlaylist:
                IEnumerable<string> playlists = iTunesHelper.GetAllPlaylists().Select(playlist => playlist.Name);
                Parameters = playlists.AddFirst("Select later").ToArray();
                break;
        }
        SelectedParameter = 0;
    }


    [ObservableProperty]
    string[] parameters = { "None" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    int selectedParameter = 0;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    Key? key = null;


    [ObservableProperty]
    HashSet<Modifier> modifiers = new();

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
            keyboardListener.PauseSubscriptions = false;
            return;
        }

        logger.LogInformation("[CreateShortcutViewModel-OnIsListeningKeyChanged] Key recording requested.");

        keyboardListener.BlockKeys = true;
        keyboardListener.ListenForAllKeys = true;
        keyboardListener.PauseSubscriptions = true;

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
}