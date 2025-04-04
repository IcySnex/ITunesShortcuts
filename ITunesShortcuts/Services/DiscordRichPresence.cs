using iTunesLib;
using DiscordRPC;
using Microsoft.Extensions.Logging;
using ITunesShortcuts.Helpers;

namespace ITunesShortcuts.Services;

public class DiscordRichPresence
{
    readonly ILogger<DiscordRichPresence> logger;
    readonly ITunesHelper iTunesHelper;
    readonly ImageUploader imageUploader;

    public DiscordRichPresence(
        ILogger<DiscordRichPresence> logger,
        ITunesHelper iTunesHelper,
        ImageUploader imageUploader)
    {
        this.logger = logger;
        this.iTunesHelper = iTunesHelper;
        this.imageUploader = imageUploader;

        logger.LogInformation("[DiscordRichPresence-.ctor] DiscordRichPresence has been initialized.");
    }


    readonly DiscordRpcClient client = new("1356662158091616548");


    RichPresence? currentPresence = null;

    public async void Update()
    {
        // Get track
        IITFileOrCDTrack? track = iTunesHelper.GetCurrentTrack();
        if (track is null)
            return;

        (bool isPlaying, TimeSpan elapsedTime) = iTunesHelper.GetPlayerState();
        if (!isPlaying)
            return;

        // Upload artwork
        string? artworkFilePath = track.GetArtworkFilePath();
        string artworkUrl = artworkFilePath is null ? "placeholder" : await imageUploader.GetAsync(artworkFilePath) ?? "placeholder";

        // Set RPC
        if (!client.IsInitialized)
        {
            client.Initialize();
            logger.LogInformation("[DiscordRichPresence-Update] Initialized Discord Rich Presence.");
        }

        currentPresence = new()
        {
            Type = ActivityType.Listening,
            Details = track.Name,
            State = track.Artist,
            Timestamps = new(
                DateTime.UtcNow - elapsedTime,
                (DateTime.UtcNow - elapsedTime).AddSeconds(track.Duration)),
            Assets = new()
            {
                LargeImageKey = artworkUrl,
                LargeImageText = track.Album,
            }

        };
        client.SetPresence(currentPresence);

        logger.LogInformation("[DiscordRichPresence-Update] Set Discord Rich Presence.");
    }
    public void UpdateTimestamps(
        TimeSpan elapsedTime)
    {
        if (currentPresence is null)
            return;

        TimeSpan? duration = (currentPresence.Timestamps.End - currentPresence.Timestamps.Start);
        if (duration is null)
            return;

        currentPresence.Timestamps = new(
            (DateTime.UtcNow - elapsedTime).AddMilliseconds(-500),
            (DateTime.UtcNow - elapsedTime).AddMilliseconds(-500) + duration.Value);
        client.SetPresence(currentPresence);

        logger.LogInformation("[DiscordRichPresence-Update] Updated Discord Rich Presence Timestamps.");
    }

    public void Clear()
    {
        currentPresence = null;
        client.SetPresence(currentPresence);

        logger.LogInformation("[DiscordRichPresence-Clear] Cleared Discord Rich Presence.");
    }
}