using iTunesLib;
using ITunesShortcuts.Enums;
using ITunesShortcuts.EventArgs;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using ITunesShortcuts.ViewModels;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

namespace ITunesShortcuts.Services;

public class ShortcutManager
{
    readonly ILogger<ShortcutManager> logger;
    readonly WindowHelper windowHelper;
    readonly JsonConverter converter;
    readonly Notifications notifications;
    readonly ITunesHelper iTunesHelper;
    readonly KeyboardListener keyboardListener;

    public ObservableRangeCollection<Shortcut> Shortcuts { get; } = new();

    public ShortcutManager(
        ILogger<ShortcutManager> logger,
        WindowHelper windowHelper,
        JsonConverter converter,
        Notifications notifications,
        ITunesHelper iTunesHelper,
        KeyboardListener keyboardListener)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;
        this.converter = converter;
        this.notifications = notifications;
        this.iTunesHelper = iTunesHelper;
        this.keyboardListener = keyboardListener;

        Shortcuts.CollectionChanged += OnShortcutsCollectionChanged;

        logger.LogInformation("[ShortcutManager-.ctor] ShortcutManager has been initialized.");
    }


    readonly string artworkLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempArtwork.jpg");

    IITFileOrCDTrack? GetCurrentTrackAndSaveArtwork()
    {
        IITFileOrCDTrack? track = iTunesHelper.GetCurrentTrack();
        if (track is null)
        {
            notifications.Send("No track is currently playing in iTunes.");
            logger.LogError("[ShortcutManager-GetCurrentTrackAndSaveArtwork] Failed to get current track: null");
            return null;
        }

        iTunesHelper.SaveArtwork(artworkLocation, track);

        return track;
    }


    Action<KeyPressedEventArgs> CreateAction(
        Shortcut shortcut) =>
        shortcut.Action switch
        {
            ShortcutAction.Get => args =>
            {
                GetAction();
            },
            ShortcutAction.Rate => args =>
            {
                switch (shortcut.Parameter)
                {
                    case "Select later":
                        RateAction();
                        break;
                    case "Reset":
                        RateAction(0);
                        break;
                    default:
                        int stars = int.Parse(shortcut.Parameter[0].ToString());
                        RateAction(stars);
                        break;
                }
            },
            ShortcutAction.AddToPlaylist => args =>
            {
                switch (shortcut.Parameter)
                {
                    case "Select later":
                        AddToPlaylistAction();
                        break;
                    default:
                        AddToPlaylistAction(shortcut.Parameter);
                        break;
                }
            },
            ShortcutAction.ViewLyrics => args =>
            {
                ViewLyrics();
            },
            ShortcutAction.ShowTrackSummary => args =>
            {
                ShowTrackSummary();
            },
            _ => args => logger.LogInformation("[ShortcutManager-Action] Key [{key}] was pressed: Action type invalid", args.Key)
        };

    void GetAction()
    {
        IITFileOrCDTrack? track = GetCurrentTrackAndSaveArtwork();
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
        logger.LogInformation("[ShortcutManager-Action] Action was invoked: Get");
    }

    void RateAction()
    {
        IITFileOrCDTrack? track = GetCurrentTrackAndSaveArtwork();
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
        logger.LogInformation("[ShortcutManager-Action] Action was invoked: Rate");
    }
    void RateAction(
        int stars)
    {
        IITFileOrCDTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        track.Rating = stars * 20;

        string info = stars == 0 ? "Reset" : $"{stars} Star{(stars != 1 ? 's' : "")}";
        notifications.Send(
            $"Rate current track ({info})\n{track.Name}\n{track.Artist} - {track.Album}",
            $"file:///{artworkLocation}");
        logger.LogInformation("[ShortcutManager-Action] Action was invoked: Rate [{stars}]", info);
    }

    void AddToPlaylistAction()
    {
        IITFileOrCDTrack? track = GetCurrentTrackAndSaveArtwork();
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
        logger.LogInformation("[ShortcutManager-Action] Action was invoked: AddToPlaylist");
    }
    void AddToPlaylistAction(
        string playlistName)
    {
        IITFileOrCDTrack? track = GetCurrentTrackAndSaveArtwork();
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
        logger.LogInformation("[ShortcutManager-Action] Action was invoked: AddToPlaylist [{playlistName}]", playlistName);
    }

    void ViewLyrics()
    {
        IITFileOrCDTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        if (string.IsNullOrWhiteSpace(track.Lyrics))
        {
            notifications.Send("Failed to view lyrics.\nError: The current track does not have any lyrics.");
            return;
        }

        windowHelper.CreateLyricsView(track, artworkLocation);

        logger.LogInformation("[ShortcutManager-Action] Action was invoked: ViewLyrics");
    }

    void ShowTrackSummary()
    {
        IITFileOrCDTrack? track = GetCurrentTrackAndSaveArtwork();
        if (track is null) return;

        TrackSummaryViewModel viewModel = new(logger, windowHelper, iTunesHelper, track, artworkLocation);
        windowHelper.CreateTrackSummaryView(viewModel);

        logger.LogInformation("[ShortcutManager-Action] Action was invoked: ShowTrackSummary");
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

            _ = Win32.MessageBox(IntPtr.Zero, $"Error: Failed to remove or unsubscribe action from key: {key.Key}", "Error", Win32.MB_OK | Win32.MB_ICONERROR);
        }

        foreach (Shortcut shortcut in Shortcuts)
        {
            if (keyboardListener.AddKey(shortcut.Key, shortcut.Modifiers) && keyboardListener.Subscribe(shortcut.Key, CreateAction(shortcut)))
                continue;

            _ = Win32.MessageBox(IntPtr.Zero, $"Error: Failed to add or subscribe action to key: {shortcut.Key}", "Error", Win32.MB_OK | Win32.MB_ICONERROR);
        }

        logger.LogInformation("[ShortcutManager-OnShortcutsCollectionChanged] Updated KeyboardListener to newest shortcuts");
    }

}