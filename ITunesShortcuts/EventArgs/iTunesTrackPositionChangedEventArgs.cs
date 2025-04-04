using iTunesLib;

namespace ITunesShortcuts.EventArgs;

public class ITunesTrackPositionChangedEventArgs
{
    public ITunesTrackPositionChangedEventArgs(
        TimeSpan oldPosition,
        TimeSpan newPosition)
    {
        OldPosition = oldPosition;
        NewPosition = newPosition;
    }


    public TimeSpan OldPosition { get; }
    public TimeSpan NewPosition { get; }
}