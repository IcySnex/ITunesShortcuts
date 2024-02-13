using iTunesLib;
using ITunesShortcuts.Enums;
using ITunesShortcuts.EventArgs;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using Microsoft.Windows.AppNotifications.Builder;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace ITunesShortcuts.Services;

public class ShortcutManager
{
    readonly ILogger<ShortcutManager> logger;
    readonly JsonConverter converter;
    readonly Notifications notifications;
    readonly ITunesHelper iTunesHelper;
    readonly KeyboardListener keyboardListener;

    public ObservableRangeCollection<Shortcut> Shortcuts { get; } = new();

    public ShortcutManager(
        ILogger<ShortcutManager> logger,
        JsonConverter converter,
        Notifications notifications,
        ITunesHelper iTunesHelper,
        KeyboardListener keyboardListener)
    {
        this.logger = logger;
        this.converter = converter;
        this.notifications = notifications;
        this.iTunesHelper = iTunesHelper;
        this.keyboardListener = keyboardListener;

        Shortcuts.CollectionChanged += OnShortcutsCollectionChanged;

        logger.LogInformation("[ShortcutManager-.ctor] ShortcutManager has been initialized.");
    }


    Action<KeyPressedEventArgs> CreateAction(
        Shortcut shortcut) =>
        shortcut.Action switch
        {
            ShortcutAction.Get => args =>
            {
                IITTrack? track = iTunesHelper.GetCurrentTrack();

                string artworkLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempArtwork.jpg");
                track?.Artwork.OfType<IITArtwork>().FirstOrDefault()?.SaveArtworkToFile(artworkLocation);

                AppNotificationBuilder? builder = track is null ?
                    new AppNotificationBuilder().AddText("No track is currently playing in iTunes.") :
                    new AppNotificationBuilder()
                        .AddText("Get current track", new AppNotificationTextProperties().SetMaxLines(1))
                        .AddText($"{track.Name}: {track.PlayedCount} times played\n" +
                            $"{track.Artist} - {track.Album}")
                        .SetAppLogoOverride(new($"file:///{artworkLocation}"));

                Action<string> onButtonClick = buttonContent =>
                {
                    switch (buttonContent)
                    {
                        case "◀":
                            iTunesHelper.BackTrack();
                            break;
                        case "❚❚":
                            iTunesHelper.PlayPause();
                            break;
                        case "▶":
                            iTunesHelper.NextTrack();
                            break;
                    }
                };

                notifications.SendWithButton(new[] { "◀", "❚❚", "▶" }, onButtonClick, builder);

                logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Get", args.Key);
            },
            _ => args =>
            {
                logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Action type invalid", args.Key);
            }
        };


    public void Load()
    {
        if (!File.Exists("shortcuts.json"))
        {
            logger.LogError("[ShortcutManager-Load] Failed to load shortcuts: File not found.");
            return;
        }

        string json = File.ReadAllText("shortcuts.json");
        Shortcut[]? shortcuts = converter.ToObject<Shortcut[]>(json);
        if (shortcuts is null)
        {
            logger.LogError("[ShortcutManager-Load] Failed to load shortcuts: Could not convert json to array.");
            return;
        }

        Shortcuts.AddRange(shortcuts);
    }

    public void Save()
    {
        string json = converter.ToString(Shortcuts);
        File.WriteAllText("shortcuts.json", json);

        logger.LogInformation("[ShortcutManager-SaveAsync] Saved all shortcuts to [{saveLocation}].", "shortcuts.json");
    }


    void OnShortcutsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        foreach (KeyValuePair<Key, HashSet<Modifier>?> key in keyboardListener.GetKeys())
        {
            if (keyboardListener.RemoveKey(key.Key) && keyboardListener.Unsubscribe(key.Key))
                continue;

            Win32.MessageBox(IntPtr.Zero, $"Error: Failed to remove or unsubscribe action from key: {key.Key}", "Error",  Win32.MB_OK | Win32.MB_ICONERROR);
        }

        foreach (Shortcut shortcut in Shortcuts)
        {
            if (keyboardListener.AddKey(shortcut.Key, shortcut.Modifiers) && keyboardListener.Subscribe(shortcut.Key, CreateAction(shortcut)))
                continue;

            Win32.MessageBox(IntPtr.Zero, $"Error: Failed to add or subscribe action to key: {shortcut.Key}", "Error", Win32.MB_OK | Win32.MB_ICONERROR);
        }

        logger.LogInformation("[ShortcutManager-OnShortcutsCollectionChanged] Updated KeyboardListener to newest shortcuts");
    }

}