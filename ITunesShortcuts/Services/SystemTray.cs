using H.NotifyIcon.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Drawing;

namespace ITunesShortcuts.Services;

public class SystemTray
{
    readonly ILogger<SystemTray> logger;
    readonly WindowHelper windowHelper;
    readonly Navigation navigation;
    readonly KeyboardListener keyboardListener;

    TrayIconWithContextMenu? icon;
    readonly public PopupMenuItem ExitItem = default!;
    readonly public PopupMenuItem ToggleShortcutsItem = default!;
    readonly public PopupMenuItem SettingsItem = default!;
    readonly public PopupMenuItem ToggleWindowItem = default!;

    public SystemTray(
        ILogger<SystemTray> logger,
        WindowHelper windowHelper,
        Navigation navigation,
        KeyboardListener keyboardListener)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;
        this.navigation = navigation;
        this.keyboardListener = keyboardListener;

        ToggleShortcutsItem = new("Disable all shortcuts", (_, _) => ToggleShortcuts());
        ToggleWindowItem = new("Hide window", (_, _) => ToggleWindow());
        SettingsItem = new("Settings", (_, _) => Settings());
        ExitItem = new("Exit", (_, _) => Exit());

        logger.LogInformation("[SystemTray-.ctor] SystemTray has been initialized.");
    }


    public void Enable()
    {
        icon = new()
        {
            Icon = new Icon("icon.ico").Handle,
            ToolTip = "iTunes Shortcuts",
            ContextMenu = new()
            {
                Items =
                {
                    ToggleShortcutsItem,
                    new PopupMenuSeparator(),
                    SettingsItem,
                    ToggleWindowItem,
                    new PopupMenuSeparator(),
                    ExitItem
                }
            }
        };

        icon.Create();
        logger.LogInformation("[SystemTray-Enable] Tray Icon has been created and enabled.");
    }

    public void Disable()
    {
        icon?.Dispose();
        icon = null;

        logger.LogInformation("[SystemTray-Disable] Tray Icon has been disabled and disposed.");
    }

    public void SetVisibility(
        bool isVisible)
    {
        icon?.UpdateVisibility(isVisible ? IconVisibility.Visible : IconVisibility.Hidden);

        logger.LogInformation("[SystemTray-SetVisibility] Tray Icon visibility has been set: {value}", isVisible);
    }


    void ToggleShortcuts()
    {
        if (ToggleShortcutsItem.Text == "Enable all shortcuts")
        {
            keyboardListener.Start();
            ToggleShortcutsItem.Text = "Disable all shortcuts";
            return;
        }

        keyboardListener.Stop();
        ToggleShortcutsItem.Text = "Enable all shortcuts";
    }

    void ToggleWindow()
    {
        if (ToggleWindowItem.Text == "Show window")
        {
            windowHelper.SetVisibility(true);
            ToggleWindowItem.Text = "Hide window";
            return;
        }

        windowHelper.SetVisibility(false);
        ToggleWindowItem.Text = "Show window";
    }

    void Settings()
    {
        if (ToggleWindowItem.Text == "Show window")
        {
            windowHelper.SetVisibility(true);
            ToggleWindowItem.Text = "Hide window";
        }

        windowHelper.EnqueueDispatcher(() => navigation.Navigate("Settings"));
    }

    void Exit()
    {
        App.Provider.GetRequiredService<AppStartupHandler>().PrepareShutdown();

        Process.GetCurrentProcess().Kill();
    }
}