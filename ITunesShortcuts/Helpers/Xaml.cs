using GlobalHotKeys.Native.Types;

namespace ITunesShortcuts.Helpers;

public class Xaml
{
    public static string ModifierToString(
        Modifiers value) =>
        value switch
        {
            Modifiers.Alt => "Alt",
            Modifiers.Control => "Ctrl",
            Modifiers.NoRepeat => "None",
            Modifiers.Shift => "Shift",
            Modifiers.Win => "Win",
            _ => "N/A",
        };

    public static string KeyToString(
        VirtualKeyCode value)
    {
        string keyString = value.ToString().Replace("VK_", "").Replace("KEY_", "");
        return char.ToUpper(keyString[0]) + keyString.Substring(1).ToLower();
    }
}