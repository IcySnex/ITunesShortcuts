using iTunesLib;

namespace ITunesShortcuts.EventArgs;

public class ITunesTrackStartedEventArgs
{
    public ITunesTrackStartedEventArgs(
        IITFileOrCDTrack track)
    {
        Track = track;
    }


    public IITFileOrCDTrack Track { get; }
}