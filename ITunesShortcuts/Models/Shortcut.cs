using GlobalHotKeys.Native.Types;

namespace ITunesShortcuts.Models;

public class Shortcut
{
    public Shortcut(
        string name,
        Modifiers modifiers,
        VirtualKeyCode key,
        string action,
        string parameter)
    {
        this.Name = name;
        this.Modifiers = modifiers;
        this.Key = key;
        this.Action = action;
        this.Parameter = parameter;
    }

    public string Name { get; }

    public Modifiers Modifiers { get; }

    public VirtualKeyCode Key { get; }

    public string Action { get; }

    public string Parameter { get; }
}