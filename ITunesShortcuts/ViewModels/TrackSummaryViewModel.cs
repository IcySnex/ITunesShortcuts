using iTunesLib;
using CommunityToolkit.Mvvm.ComponentModel;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Runtime.InteropServices;

namespace ITunesShortcuts.ViewModels;

public partial class TrackSummaryViewModel : ObservableObject, IDisposable
{
    readonly ILogger logger;
    readonly WindowHelper windowHelper;
    readonly ITunesHelper iTunesHelper;

    public IITFileOrCDTrack Track { get; set; }
    readonly string artworkLocation;

    public TrackSummaryViewModel(
        ILogger logger,
        WindowHelper windowHelper,
        ITunesHelper iTunesHelper,
        IITFileOrCDTrack track,
        string artworkLocation)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;
        this.iTunesHelper = iTunesHelper;
        this.Track = track;
        this.artworkLocation = artworkLocation;

        Rating = Track.Rating == 0 ? -1 : Track.Rating / 20;
        Playlists = iTunesHelper.GetAllPlaylists().Select(playlist => playlist.Name).ToArray();
        SelectedPlaylist = -1;

        AddToPlaylistButtonBackgroundBrush = (SolidColorBrush)App.Current.Resources["ButtonBackground"];

        logger.LogInformation("[TrackSummaryViewModel-.ctor] TrackSummaryViewModel has been initialized.");
    }

    ~TrackSummaryViewModel()
    {
        Dispose(false);

        logger.LogInformation("[TrackSummaryViewModel-] TrackSummaryViewModel has been deinitialized.");
    }


    protected virtual void Dispose(
        bool explicitly)
    {
        if (disposedValue)
            return;

        if (explicitly)
        {
            // Managed
        }

        // Unmanaged
        Marshal.ReleaseComObject(Track);
        Track = default!;
        
        GC.Collect();
        GC.WaitForPendingFinalizers();

        disposedValue = true;
        logger.LogInformation("[TrackSummaryViewModel-Dispose] TrackSummaryViewModel has been disposed [{explicitly}].", explicitly);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    public string Artwork => artworkLocation;

    public string[] Playlists { get; set; } = default!;

    public bool HasLyrics => !string.IsNullOrWhiteSpace(Track.Lyrics);


    [RelayCommand]
    void Close() =>
        windowHelper.TrackSummaryView?.Close();


    [RelayCommand]
    void PlayPause() =>
        iTunesHelper.PlayPause();

    [RelayCommand]
    void BackTrack() =>
        iTunesHelper.BackTrack();
    [RelayCommand]
    void NextTrack() =>
        iTunesHelper.NextTrack();


    [ObservableProperty]
    int rating;

    partial void OnRatingChanged(
        int value)
    {
        Track.Rating = value == -1 ? 0 : value * 20;

        logger.LogInformation("[TrackSummaryViewModel-OnRatingChanged] Rated track [{stars}]", value);
    }


    [ObservableProperty]
    int selectedPlaylist = -1;

    [ObservableProperty]
    SolidColorBrush addToPlaylistButtonBackgroundBrush;
    private bool disposedValue;

    async void SetBackgroundColor(
        SolidColorBrush brush)
    {
        AddToPlaylistButtonBackgroundBrush = brush;
        await Task.Delay(3000);
        AddToPlaylistButtonBackgroundBrush = (SolidColorBrush)App.Current.Resources["ButtonBackground"];
    }

    [RelayCommand]
    void AddToPlaylist()
    {
        if (SelectedPlaylist < 0)
        {
            SetBackgroundColor(new(Color.FromArgb(50, 255, 153, 164)));
            return;
        }

        IEnumerable<IITUserPlaylist> playlists = iTunesHelper.GetAllPlaylists();
        IITUserPlaylist? playlist = playlists.FirstOrDefault(playlist => playlist.Name == Playlists[SelectedPlaylist]);
        if (playlist is null)
        {
            SetBackgroundColor(new(Color.FromArgb(50, 255, 153, 164)));
            return;
        }

        playlist.AddTrack(Track);
        SetBackgroundColor(new(Color.FromArgb(50, 108, 203, 95)));

        logger.LogInformation("[TrackSummaryViewModel-AddToPlaylist] Added track to playlist [{playlist}]", playlist.Name);
    }


    [RelayCommand]
    void ShowLyrics() =>
        windowHelper.CreateLyricsView(Track, artworkLocation);
}