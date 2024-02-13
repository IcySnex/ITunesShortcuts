using ITunesShortcuts.Enums;

namespace ITunesShortcuts.Helpers;

public static class Extensions
{
    public static Key? ToKey(
        this Modifier modifier) =>
        Enum.IsDefined(typeof(Key), (int)modifier) ? (Key)modifier : null;

    public static bool IsModifier(
        this Key key) =>
        key == Key.LMenu || key == Key.RMenu ||
        key == Key.LControl || key == Key.RControl ||
        key == Key.LShift || key == Key.RShift ||
        key == Key.LWin || key == Key.RWin;


    public static IEnumerable<T> AddFirst<T>(
        this IEnumerable<T> source,
        T element)
    {
        LinkedList<T> linkedList = new(source);
        linkedList.AddFirst(element);

        return linkedList;
    }
}