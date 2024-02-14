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

    public string Name { get; set; }

    public Key Key { get; set; }

    public HashSet<Modifier>? Modifiers { get; set; }

    public ShortcutAction Action { get; set; }

    public string Parameter { get; set; }
}