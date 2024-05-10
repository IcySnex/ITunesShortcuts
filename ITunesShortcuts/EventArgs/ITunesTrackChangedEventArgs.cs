using iTunesLib;

namespace ITunesShortcuts.EventArgs;

public class ITunesTrackChangedEventArgs
{
    public ITunesTrackChangedEventArgs(
        IITFileOrCDTrack? oldTrack,
        IITFileOrCDTrack? newTrack)
    {
        OldTrack = oldTrack;
        NewTrack = newTrack;
    }


    public IITFileOrCDTrack? OldTrack { get; }

    public IITFileOrCDTrack? NewTrack { get; }
}