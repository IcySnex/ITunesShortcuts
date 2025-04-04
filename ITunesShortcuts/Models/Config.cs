using CommunityToolkit.Mvvm.ComponentModel;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace ITunesShortcuts.Models;

public partial class Config : ObservableObject
{
    [ObservableProperty]
    bool isAutoStartEnabled = false;

    partial void OnIsAutoStartEnabledChanged(
        bool value)
    {
        string shortcutLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ITunesShortcuts.lnk");
        string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string executableName = "ITunesShortcuts.exe";

        if (!value && File.Exists(shortcutLocation))
        {
            File.Delete(shortcutLocation);
            return;
        }

        if (File.Exists(shortcutLocation))
            return;

        IWshShortcut shortcut = (IWshShortcut)new WshShell().CreateShortcut(shortcutLocation);
        shortcut.TargetPath = Path.Combine(executableDirectory, executableName);
        shortcut.WorkingDirectory = executableDirectory;
        shortcut.Description = "Run iTunesShortcuts at Windows startup";
        shortcut.Save();
    }

    [ObservableProperty]
    bool launchMinimized = false;

    [ObservableProperty]
    bool minimizeToTray = false;

    [ObservableProperty]
    bool showTrackSummary = false;

    [ObservableProperty]
    bool discordRichPresence = true;
}