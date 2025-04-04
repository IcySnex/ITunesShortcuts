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

    readonly DiscordRpcClient client = new("1356662158091616548");

    public DiscordRichPresence(
        ILogger<DiscordRichPresence> logger,
        ITunesHelper iTunesHelper,
        ImageUploader imageUploader)
    {
        this.logger = logger;
        this.iTunesHelper = iTunesHelper;
        this.imageUploader = imageUploader;

        client.Initialize();

        logger.LogInformation("[DiscordRichPresence-.ctor] DiscordRichPresence has been initialized.");
    }

    Task? backgroundLoop;
    CancellationTokenSource cancellation = new();

    RichPresence? currentPresence = null;

    int lastKnownId = 0;

    async Task UpdatePresenceAsync(
        CancellationToken token)
    {
        IITFileOrCDTrack? track = iTunesHelper.GetCurrentTrack();
        if (track is null) // No track is playing
            return;

        var (isPlaying, playerPosition) = iTunesHelper.GetPlayerState();
        if (!isPlaying) // Player is paused
            return;

        // Check if player position changed
        DateTime startTime = DateTime.UtcNow.AddSeconds(-playerPosition - 0.5);
        DateTime endTime = startTime.AddSeconds(track.Duration);

        if (currentPresence is not null && track.TrackDatabaseID == lastKnownId)
        {
            if (Math.Abs((currentPresence.Timestamps.Start - startTime)!.Value.TotalSeconds) > 3)
            {
                currentPresence.Timestamps = new(startTime, endTime);
                client.SetPresence(currentPresence);

                logger.LogInformation("[DiscordRichPresence] Updated RPC timestamps.");
            }

            return;
        }

        // Set new RPC
        string? artworkFilePath = track.GetArtworkFilePath();
        string artworkUrl = artworkFilePath is null ? "placeholder" : await imageUploader.GetAsync(artworkFilePath, token) ?? "placeholder";

        if (token.IsCancellationRequested)
            return;

        currentPresence = new()
        {
            Type = ActivityType.Listening,
            Details = track.Name,
            State = track.Artist,
            Timestamps = new(startTime, endTime),
            Assets = new()
            {
                LargeImageKey = artworkUrl,
                LargeImageText = track.Album,
            }

        };
        client.SetPresence(currentPresence);

        lastKnownId = track.TrackDatabaseID;

        logger.LogInformation("[DiscordRichPresence-OnPollingTimerElapsed] Updated RPC.");
    }


    public bool Enabled
    {
        get => backgroundLoop is not null;
        set
        {
            if (value)
            {
                if (backgroundLoop is not null)
                    return;

                cancellation = new();
                backgroundLoop = Task.Run(async () =>
                {
                    while (!cancellation.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await UpdatePresenceAsync(cancellation.Token);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "[DiscordRichPresence-LoopAsync] Failed to update RPC: {exception}", ex.Message);
                        }

                        await Task.Delay(1000, cancellation.Token);
                    }
                });
            }
            else
            {
                if (backgroundLoop is null)
                    return;

                cancellation.Cancel();
                backgroundLoop = null;

                currentPresence = null;
                client.SetPresence(currentPresence);

                lastKnownId = 0;

                logger.LogInformation("[DiscordRichPresence-SetEnabled] Cleared RPC.");
            }
        }
    }
}