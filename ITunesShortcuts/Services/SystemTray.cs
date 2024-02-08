using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace ITunesShortcuts.Services;

public class SystemTray
{
    readonly ILogger<SystemTray> logger;
    readonly WindowHelper windowHelper;

    TrayIconWithContextMenu? icon;
    readonly PopupMenuItem exitItem = default!;
    readonly PopupMenuItem toggleShortcutsItem = default!;
    readonly PopupMenuItem settingsItem = default!;
    readonly PopupMenuItem toogleWindowItem = default!;

    public SystemTray(
        ILogger<SystemTray> logger,
        WindowHelper windowHelper)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;

        toggleShortcutsItem = new("Disable all shortcuts", (_, _) => ToggleShortcuts());
        toogleWindowItem = new("Hide window", (_, _) => ToogleWindow());
        settingsItem = new("Settings", (_, _) => Settings());
        exitItem = new("Exit", (_, _) => Exit());

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
                    toggleShortcutsItem,
                    new PopupMenuSeparator(),
                    settingsItem,
                    toogleWindowItem,
                    new PopupMenuSeparator(),
                    exitItem
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
        bool isVisiblie)
    {
        icon?.UpdateVisibility(isVisiblie ? IconVisibility.Visible : IconVisibility.Hidden);

        logger.LogInformation("[SystemTray-SetVisibility] Tray Icon visibility has been set: {value}", isVisiblie);
    }


    public void ToggleShortcuts()
    {
        if (toggleShortcutsItem.Text == "Enable all shortcuts")
        {
            // Enable
            toggleShortcutsItem.Text = "Disable all shortcuts";
            return;
        }

        // Disable
        toggleShortcutsItem.Text = "Enable all shortcuts";
    }

    public void ToogleWindow()
    {
        if (toogleWindowItem.Text == "Show window")
        {
            windowHelper.SetVisibility(true);
            toogleWindowItem.Text = "Hide window";
            return;
        }

        windowHelper.SetVisibility(false);
        toogleWindowItem.Text = "Show window";
    }

    public void Settings() =>
        throw new NotImplementedException();

    public void Exit() =>
        windowHelper.Close();
}