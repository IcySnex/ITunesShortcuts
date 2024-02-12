using ITunesShortcuts.Enums;

namespace ITunesShortcuts.EventArgs;

public class KeyPressedEventArgs
{
    public KeyPressedEventArgs(
        Key key,
        HashSet<Modifier>? modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }


    public Key Key { get; }

    public HashSet<Modifier>? Modifiers { get; }
}