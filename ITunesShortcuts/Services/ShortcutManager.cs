using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using Microsoft.Extensions.Logging;

namespace ITunesShortcuts.Services;

public class ShortcutManager
{
    readonly ILogger<ShortcutManager> logger;
    readonly JsonConverter converter;

    public ObservableRangeCollection<Shortcut> Shortcuts { get; } = new();

    public ShortcutManager(
        ILogger<ShortcutManager> logger,
        JsonConverter converter)
    {
        this.logger = logger;
        this.converter = converter;

        logger.LogInformation("[ShortcutManager-.ctor] ShortcutManager has been initialized.");
    }

    public void Load()
    {
        if (!File.Exists("shortcuts.json"))
        {
            logger.LogError("[ShortcutManager-Load] Failed to load shortcuts: File not found.");
            return;
        }

        string json = File.ReadAllText("shortcuts.json");
        Shortcut[]? shortcuts = converter.ToObject<Shortcut[]>(json);
        if (shortcuts is null)
        {
            logger.LogError("[ShortcutManager-Load] Failed to load shortcuts: Could not convert json to array.");
            return;
        }

        Shortcuts.AddRange(shortcuts);
    }

    public void Save()
    {
        string json = converter.ToString(Shortcuts);
        File.WriteAllText("shortcuts.json", json);

        logger.LogInformation("[ShortcutManager-SaveAsync] Saved all shortcuts to [{saveLocation}].", "shortcuts.json");
    }
}