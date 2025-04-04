using iTunesLib;
using ITunesShortcuts.Enums;
using ITunesShortcuts.Services;
using System.Drawing.Imaging;
using System.Drawing;

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


    public static string? GetArtworkFilePath(
        this IITFileOrCDTrack track)
    {
        string filePath = Path.Combine(ImageUploader.ArtworksDirctory, $"{track.TrackDatabaseID}.jpg");
        if (File.Exists(filePath))
            return filePath;

        IITArtwork? artwork = track.Artwork.OfType<IITArtwork>().FirstOrDefault();
        if (artwork is null)
            return null;
        artwork.SaveArtworkToFile(filePath + "_temp");

        using (Bitmap resized = new(128, 128))
        using (Image image = Image.FromFile(filePath + "_temp"))
        using (Graphics graphics = Graphics.FromImage(resized))
        {
            double scaleX = 128.0 / image.Width;
            double scaleY = 128.0 / image.Height;
            double scale = Math.Max(scaleX, scaleY);

            int newWidth = (int)(image.Width * scale);
            int newHeight = (int)(image.Height * scale);
            int xOffset = (128 - newWidth) / 2;
            int yOffset = (128 - newHeight) / 2;

            graphics.DrawImage(image, xOffset, yOffset, newWidth, newHeight);
            resized.Save(filePath, ImageFormat.Jpeg);
        }

        File.Delete(filePath + "_temp");
        return filePath;
    }
}