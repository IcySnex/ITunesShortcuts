using iTunesLib;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using WinRT.Interop;

namespace ITunesShortcuts.Services;

/// <summary>
/// Helps maintaining windows
/// </summary>
public class WindowHelper
{
    readonly MainView mainView;
    readonly AppWindow window;
    readonly ILogger<WindowHelper> logger;

    /// <summary>
    /// Creates a new WindowHelper with with optional logging functions
    /// </summary>
    /// <param name="window">The window which is used in the helper</param>
    /// <param name="logger">The logger which will be used for logging</param>
    public WindowHelper(
        MainView mainView,
        ILogger<WindowHelper> logger)
    {
        HWnd = GetHWnd(mainView);
        window = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(HWnd));

        this.mainView = mainView;
        this.logger = logger;

        logger.LogInformation("[WindowHelper-.ctor] WindowHelper has been initialized.");
    }


    /// <summary>
    /// Gets the HWND of an owner window
    /// </summary>
    /// <param name="owner">The owner window</param>
    /// <returns></returns>
    public static IntPtr GetHWnd(
        Window owner) =>
        WindowNative.GetWindowHandle(owner);


    /// <summary>
    /// HWND of the current main window
    /// </summary>
    public readonly IntPtr HWnd;


    /// <summary>
    /// Active logger window (null if none is active)
    /// </summary>
    public LoggerView? LoggerView = null;

    /// <summary>
    /// Creates and activates a new window which hooks to the logger events
    /// </summary>
    public void CreateLoggerView()
    {
        if (LoggerView is not null)
        {
            LoggerView.Activate();
            return;
        }

        LoggerView = new() { Title = "iTunes Shortcuts (Logger)" };

        void handler(object? s, string e) =>
            LoggerView.ContentBlock.Text += e;

        App.Sink.OnNewLog += handler;
        LoggerView.Closed += (s, e) =>
        {
            App.Sink.OnNewLog -= handler;
            LoggerView = null;
        };

        WindowId id = Win32Interop.GetWindowIdFromWindow(GetHWnd(LoggerView));
        AppWindow window = AppWindow.GetFromWindowId(id);

        window.Resize(new(700, 400));

        LoggerView.Activate();

        logger.LogInformation("[WindowHelper-CreateLoggerView] Created new LoggerView and hooked handler");
    }


    public LyricsView? LyricsView = null;

    public void CreateLyricsView(
        IITFileOrCDTrack track,
        string artworkLocation)
    {
        LyricsView?.Close();

        LyricsView = new(track, artworkLocation);
        LyricsView.Closed += (s, e) =>
        {
            LyricsView = null;
        };

        WindowId id = Win32Interop.GetWindowIdFromWindow(GetHWnd(LyricsView));
        DisplayArea display = DisplayArea.GetFromWindowId(id, DisplayAreaFallback.Nearest);

        AppWindow window = AppWindow.GetFromWindowId(id);
        window.SetIcon("icon.ico");
        window.Resize(new(500, 700));
        window.Move(new(display.WorkArea.Width - 512, display.WorkArea.Height - 712));

        OverlappedPresenter presenter = (OverlappedPresenter)window.Presenter;
        presenter.SetBorderAndTitleBar(true, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsAlwaysOnTop = true;

        LyricsView.Activate();

        logger.LogInformation("[WindowHelper-CreateLyricsView] Created new LyricsView and hooked handler");
    }


    /// <summary>
    /// Initializes a target with the current main window
    /// </summary>
    /// <param name="target">The target</param>
    public void Initialize(
        object target) =>
        InitializeWithWindow.Initialize(target, HWnd);


    /// <summary>
    /// Tries to enqueue a callback to the current main views dispatcher queue
    /// </summary>
    public void EnqueueDispatcher(
        DispatcherQueueHandler callback) =>
        mainView.DispatcherQueue.TryEnqueue(callback);


    /// <summary>
    /// Sets the visibility of the current main window
    /// </summary>
    /// <param name="isVisible">Whether to hide or show the window</param>
    public void SetVisibility(
        bool isVisible)
    {
        if (isVisible)
        {
            window.Show(true);
            Win32.SetForegroundWindow(HWnd);
            return;
        }

        window.Hide();
    }

    /// <summary>
    /// Sets a custom icon on the current main window
    /// </summary>
    /// <param name="path">The path to the icon</param>
    public void SetIcon(
        string path)
    {
        window.SetIcon(path);

        logger?.LogInformation("[WindowHelper-SetIcon] Set app icon to bitmap");
    }

    /// <summary>
    /// Sets the minimum size of the current main window
    /// </summary>
    /// <param name="width">The width of the new minimum size</param>
    /// <param name="height">The height of the new minimum size</param>
    public void SetMinSize(
        int width,
        int height)
    {
        IntPtr dpi = Win32.GetDpiForWindow(HWnd);

        Win32.MinWidth = (int)(width * (float)dpi / 96);
        Win32.MinHeight = (int)(height * (float)dpi / 96);

        Win32.NewWndProc = new Win32.WinProc(Win32.NewWindowProc);
        Win32.OldWndProc = Win32.SetWindowLongAll(HWnd, -16 | 0x4 | 0x8, Win32.NewWndProc);

        logger.LogInformation("[WindowHelper-SetMinSize] Set minimum window size [{width}x{height}]", width, height);
    }

    /// <summary>
    /// Sets the size of the current main window
    /// </summary>
    /// <param name="width">The width of the new size</param>
    /// <param name="height">The height of the new size</param>
    public void SetSize(
        int width,
        int height)
    {
        window.Resize(new(width + 16, height + 39));

        logger.LogInformation("[WindowHelper-SetSize] Set window size [{width}x{height}]", width, height);
    }


    /// <summary>
    /// Displays a ContentDialog
    /// </summary>
    /// <param name="dialog">The dialog to display</param>
    public IAsyncOperation<ContentDialogResult> AlertAsync(
        ContentDialog dialog)
    {
        foreach (Popup popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(mainView.Content.XamlRoot))
            if (popup.Child is ContentDialog openDialog)
                openDialog.Hide();

        dialog.XamlRoot = mainView.Content.XamlRoot;
        dialog.PrimaryButtonStyle = (Style)App.Current.Resources["AccentButtonStyle"];
        dialog.Style = (Style)App.Current.Resources["DefaultContentDialogStyle"];


        return dialog.ShowAsync();
    }

    /// <summary>
    /// Creates a new ContentDialog which displays the content and title and logs the message
    /// </summary>
    /// <param name="content">The content of the dialog</param>
    /// <param name="title">The title of the dialog</param>
    public IAsyncOperation<ContentDialogResult> AlertAsync(
        object content,
        string? title = null,
        string? closeButton = "Ok",
        string? primaryButton = null)
    {
        ContentDialog dialog = new()
        {
            Content = content,
            Title = title,
            CloseButtonText = closeButton,
            PrimaryButtonText = primaryButton
        };
        return AlertAsync(dialog);
    }
}