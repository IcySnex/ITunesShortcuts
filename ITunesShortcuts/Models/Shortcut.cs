using ITunesShortcuts.Enums;

namespace ITunesShortcuts.Models;

public class Shortcut
{
    public Shortcut(
        string name,
        Key key,
        HashSet<Modifier>? modifiers,
        ShortcutAction action,
        string parameter)
    {
        this.Name = name;
        this.Key = key;
        this.Modifiers = modifiers;
        this.Action = action;
        this.Parameter = parameter;
    }

    public string Name { get; }

    public Key Key { get; }

    public HashSet<Modifier>? Modifiers { get; }

    public ShortcutAction Action { get; }

    public string Parameter { get; }
}