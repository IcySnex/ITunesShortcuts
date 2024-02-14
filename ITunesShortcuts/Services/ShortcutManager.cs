using iTunesLib;
using ITunesShortcuts.Enums;
using ITunesShortcuts.EventArgs;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

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
            notifications.Send("No track is currently playing in iTunes.");
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
                switch (shortcut.Parameter)
                {
                    case "Select later":
                        RateAction(args);
                        break;
                    case "Reset":
                        RateAction(args, 0);
                        break;
                    default:
                        int stars = int.Parse(shortcut.Parameter[0].ToString());
                        RateAction(args, stars);
                        break;
                }
            },
            ShortcutAction.AddToPlaylist => args =>
            {
                switch (shortcut.Parameter)
                {
                    case "Select later":
                        AddToPlaylistAction(args);
                        break;
                    default:
                        AddToPlaylistAction(args, shortcut.Parameter);
                        break;
                }
            },
            _ => args => logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Action type invalid", args.Key)
        };

    void GetAction(
        KeyPressedEventArgs args)
    {
        IITTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

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

            logger.LogInformation("[ShortcutManager-onButtonClick] Controlled media: {action}", buttonContent);
        }

        notifications.SendWithButton(new[] { "◀", "❚❚", "▶" }, onButtonClick,
            $"Get current track\n{track.Name}\n{track.Artist} - {track.Album}",
            $"file:///{artworkLocation}");
        logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Get", args.Key);
    }

    void RateAction(
        KeyPressedEventArgs args)
    {
        IITTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        void onButtonClick(string buttonContent)
        {
            int stars = int.Parse(buttonContent[0].ToString());
            track.Rating = stars * 20;

            logger.LogInformation("[ShortcutManager-onButtonClick] Rated current track: {stars}", stars);
        }

        notifications.SendWithButton(
            new[] { "1 ★", "2 ★", "3 ★", "4 ★", "5 ★" },
            onButtonClick, 
            $"Rate current track\n{track.Name}\n{track.Artist} - {track.Album}",
            $"file:///{artworkLocation}");
        logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Rate", args.Key);
    }

    void RateAction(
        KeyPressedEventArgs args,
        int stars)
    {
        IITTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        track.Rating = stars * 20;

        string info = stars == 0 ? "Reset" : $"{stars} Star{(stars != 1 ? 's' : "")}";
        notifications.Send(
            $"Rate current track ({info})\n{track.Name}\n{track.Artist} - {track.Album}",
            $"file:///{artworkLocation}");
        logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Rate [{stars}]", args.Key, info);
    }

    void AddToPlaylistAction(
        KeyPressedEventArgs args)
    {
        IITTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        IEnumerable<IITUserPlaylist> playlists = iTunesHelper.GetAllPlaylists();

        void onComboBoxSelectItem(string? item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                notifications.Send("Failed to add current track to playlist.\nError: There was no playlist selected.");
                return;
            }

            IITUserPlaylist? playlist = playlists.FirstOrDefault(playlist => playlist.Name == item);
            if (playlist is null)
            {
                notifications.Send("Failed to add current track to playlist.\nError: The selected playlist could not be found.");
                return;
            }

            playlist.AddTrack(track);

            logger.LogInformation("[ShortcutManager-onButtonClick] Added track to playlist: {playlist}", item);
        }

        notifications.SendWithComboBox(
            playlists.Select(playlist => playlist.Name).ToArray(),
            onComboBoxSelectItem,
            $"Add current track to playlist\n{track.Name}\n{track.Artist} - {track.Album}",
            $"file:///{artworkLocation}");
        logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: AddToPlaylist", args.Key);
    }

    void AddToPlaylistAction(
        KeyPressedEventArgs args,
        string playlistName)
    {
        IITTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        IEnumerable<IITUserPlaylist> playlists = iTunesHelper.GetAllPlaylists();

        IITUserPlaylist? playlist = playlists.FirstOrDefault(playlist => playlist.Name == playlistName);
        if (playlist is null)
        {
            notifications.Send("Failed to add current track to playlist.\nError: The selected playlist could not be found.");
            return;
        }

        playlist.AddTrack(track);

        notifications.Send(
            $"Rate current track ({playlistName}, {playlist.Tracks.Count} tracks)\n{track.Name}\n{track.Artist} - {track.Album}",
            $"file:///{artworkLocation}");
        logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: AddToPlaylist [{playlistName}]", args.Key, playlistName);
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