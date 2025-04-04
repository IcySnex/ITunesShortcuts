using iTunesLib;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace ITunesShortcuts.Views;

public sealed partial class LyricsView : Window
{
    public LyricsView(
        IITFileOrCDTrack track,
        string? artworkLocation)
    {
        Name = track.Name;
        Artist = track.Artist;
        Album = track.Album;
        Lyrics = track.Lyrics;
        ArtworkLocation = artworkLocation;
        Artwork = artworkLocation is null ? null : new(new(artworkLocation))
        {
            CreateOptions = BitmapCreateOptions.IgnoreImageCache
        };

        this.InitializeComponent();
    }

    void OnCloseButtonClick(object _, RoutedEventArgs _1) =>
        this.Close();


    public string Name { get; }

    public string Artist { get; }

    public string Album { get; }

    public string Lyrics { get; }

    public string? ArtworkLocation { get; }

    public BitmapImage? Artwork { get; }


    CanvasBitmap? image = null;
    GaussianBlurEffect blurEffect = default!;

    ContainerVisual container = default!;
    SpriteVisual visual = default!;
    CompositionLinearGradientBrush gradientMask = default!;

    RenderTargetBitmap renderTargetBitmap = default!;
    int dpi = default!;


    async Task PrepareCanvasAsync(
        CanvasControl canvas)
    {
        if (ArtworkLocation is null)
            return;

        using (IRandomAccessStream fileStream = File.OpenRead(ArtworkLocation).AsRandomAccessStream())
            image = await CanvasBitmap.LoadAsync(canvas, fileStream);

        blurEffect = new()
        {
            Source = image,
            BlurAmount = 85.0f,
            BorderMode = EffectBorderMode.Hard
        };

        container = ElementCompositionPreview.GetElementVisual(ScrollOverlay).Compositor.CreateContainerVisual();
        ElementCompositionPreview.SetElementChildVisual(ScrollOverlay, container);

        visual = container.Compositor.CreateSpriteVisual();
        container.Children.InsertAtTop(visual);

        gradientMask = container.Compositor.CreateLinearGradientBrush();
        gradientMask.StartPoint = new(0, 0);
        gradientMask.EndPoint = new(0, 1);
        gradientMask.ColorStops.Add(container.Compositor.CreateColorGradientStop(0, Color.FromArgb(255, 0, 0, 0)));
        gradientMask.ColorStops.Add(container.Compositor.CreateColorGradientStop(1, Color.FromArgb(0, 0, 0, 0)));

        renderTargetBitmap = new();
        dpi = (int)Win32.GetDpiForWindow(WindowHelper.GetHWnd(this));

        canvas.Invalidate();
    }


    void RenderScrollOverlay(
        IRandomAccessStream pixelsStream)
    {
        LoadedImageSurface loadedImageSurface = LoadedImageSurface.StartLoadFromStream(pixelsStream);
        CompositionSurfaceBrush imageBrush = container.Compositor.CreateSurfaceBrush();
        imageBrush.Surface = loadedImageSurface;

        CompositionMaskBrush brush = container.Compositor.CreateMaskBrush();
        brush.Source = imageBrush;
        brush.Mask = gradientMask;

        visual.Brush = brush;
        visual.Size = new((float)loadedImageSurface.DecodedPhysicalSize.Width, (float)loadedImageSurface.DecodedPhysicalSize.Height);
    }

    async void OnCanvasDraw(
        CanvasControl sender,
        CanvasDrawEventArgs args)
    {
        if (image is null)
        {
            await PrepareCanvasAsync(sender);
            return;
        }

        double scale = Math.Max(sender.ActualWidth / image.Size.Width, sender.ActualHeight / image.Size.Height);

        double width = image.Size.Width * scale;
        double height = image.Size.Height * scale;
        double offsetX = (sender.ActualWidth - width) / 2;
        double offsetY = (sender.ActualHeight - height) / 2;

        CanvasLinearGradientBrush gradientBrush = new(args.DrawingSession, new CanvasGradientStop[]
        {
            new(0, Color.FromArgb(70, 20, 20, 20)),
            new(0.3f, Color.FromArgb(150, 20, 20, 20)),
            new(0.7f, Color.FromArgb(180, 20, 20, 20)),
        })
        {
            StartPoint = new(0, 0),
            EndPoint = new(0, (float)sender.ActualHeight)
        };

        args.DrawingSession.DrawImage(blurEffect, new Rect(offsetX, offsetY, width, height), image.Bounds);
        args.DrawingSession.FillRectangle(0, 0, (float)sender.ActualWidth, (float)sender.ActualHeight, gradientBrush);


        await renderTargetBitmap.RenderAsync(sender);
        IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

        InMemoryRandomAccessStream encodedStream = new();
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, encodedStream);

        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Straight,
            (uint)renderTargetBitmap.PixelWidth,
            (uint)renderTargetBitmap.PixelHeight,
            dpi, dpi,
            pixelBuffer.ToArray());
        encoder.BitmapTransform.Bounds = new(0, 116, (uint)sender.ActualWidth - 14, 24);

        await encoder.FlushAsync();
        RenderScrollOverlay(encodedStream);
    }
}
