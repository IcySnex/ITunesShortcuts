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


    readonly string artworkLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempArtwork.jpg");

    IITTrack? GetCurrentTrackAndSaveArtwork()
    {
        IITTrack? track = iTunesHelper.GetCurrentTrack();
        if (track is null)
        {
            notifications.Send(Notifications.CreateBuilder("No track is currently playing in iTunes."));
            logger.LogError("[ShortcutManager-GetCurrentTrackAndSaveArtwork] Failed to get current track: null");
            return null;
        }

        track.Artwork.OfType<IITArtwork>().FirstOrDefault()?.SaveArtworkToFile(artworkLocation);
        logger.LogInformation("[ShortcutManager-GetCurrentTrackAndSaveArtwork] Saved artwork to temp file");

        return track;
    }


    Action<KeyPressedEventArgs> CreateAction(
        Shortcut shortcut) =>
        shortcut.Action switch
        {
            ShortcutAction.Get => GetAction,
            ShortcutAction.Rate => args =>
            {
                if (shortcut.Parameter == "Select later")
                    RateAction(args);
                else
                    RateAction(args, int.Parse(shortcut.Parameter[0].ToString()));
            },
            _ => args => logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Action type invalid", args.Key)
        };

    void GetAction(
        KeyPressedEventArgs args)
    {
        IITTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        AppNotificationBuilder builder = Notifications.CreateBuilder(
                "Get current track", track.Name, $"{track.Artist} - {track.Album}")
                .SetAppLogoOverride(new($"file:///{artworkLocation}"));

        void onButtonClick(string buttonContent)
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

            logger.LogInformation("[ShortcutManager-onButtonClick] Controlled media [{action}]: {track}", buttonContent, track.Name);
        }

        notifications.SendWithButton(new[] { "◀", "❚❚", "▶" }, onButtonClick, builder);
        logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Get", args.Key);
    }

    void RateAction(
        KeyPressedEventArgs args)
    {
        IITTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        AppNotificationBuilder builder = Notifications.CreateBuilder(
                "Rate current track", track.Name, $"{track.Artist} - {track.Album}")
                .SetAppLogoOverride(new($"file:///{artworkLocation}"));

        void onButtonClick(string buttonContent)
        {
            int stars = int.Parse(buttonContent[0].ToString());
            track.Rating = stars * 20;

            logger.LogInformation("[ShortcutManager-onButtonClick] Rated current track [{stars}]: {track}", stars, track.Name);
        }

        notifications.SendWithButton(new[] { "1 ★", "2 ★", "3 ★", "4 ★", "5 ★" }, onButtonClick, builder);
        logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Rate", args.Key);
    }

    void RateAction(
        KeyPressedEventArgs args,
        int stars)
    {
        IITTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        AppNotificationBuilder builder = Notifications.CreateBuilder(
                $"Rate current track ({stars} Star{(stars != 1 ? 's' : "")})", track.Name, $"{track.Artist} - {track.Album}")
                .SetAppLogoOverride(new($"file:///{artworkLocation}"));

        track.Rating = stars * 20;

        notifications.Send(builder);
        logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Rate [{stars}]", args.Key, stars);
    }


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