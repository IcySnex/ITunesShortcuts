using ITunesShortcuts.ViewModels;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;

namespace ITunesShortcuts.Views;

public sealed partial class TrackSummaryView : UserControl
{
    readonly TrackSummaryViewModel viewModel;

    public TrackSummaryView(
        TrackSummaryViewModel viewModel)
    {
        this.viewModel = viewModel;

        this.InitializeComponent();
    }


    CanvasBitmap? image = null;
    GaussianBlurEffect blurEffect = default!;


    async Task PrepareCanvasAsync(
        CanvasControl canvas)
    {
        if (viewModel.Artwork is null)
            return;

        using (IRandomAccessStream fileStream = File.OpenRead(viewModel.Artwork).AsRandomAccessStream())
            image = await CanvasBitmap.LoadAsync(canvas, fileStream);

        blurEffect = new()
        {
            Source = image,
            BlurAmount = 85.0f,
            BorderMode = EffectBorderMode.Hard
        };

        canvas.Invalidate();
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
    }
}
