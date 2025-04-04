using iTunesLib;

namespace ITunesShortcuts.EventArgs;

public class ITunesTrackStoppedEventArgs
{
    public ITunesTrackStoppedEventArgs(
        IITFileOrCDTrack track)
    {
        Track = track;
    }


    public IITFileOrCDTrack Track { get; }
}