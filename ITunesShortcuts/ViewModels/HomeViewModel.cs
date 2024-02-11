﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Models;
using ITunesShortcuts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace ITunesShortcuts.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    readonly ILogger<HomeViewModel> logger;
    readonly WindowHelper windowHelper;
    readonly Navigation navigation;

    public ShortcutManager ShortcutManager { get; }

    public HomeViewModel(
        ILogger<HomeViewModel> logger,
        WindowHelper windowHelper,
        Navigation navigation,
        ShortcutManager shortcutManager)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;
        this.navigation = navigation;
        this.ShortcutManager = shortcutManager;

        logger.LogInformation("[HomeViewModel-.ctor] HomeViewModel has been initialized.");
    }


    [RelayCommand]
    async Task CreateShortcutAsync()
    {
        if (await windowHelper.AlertAsync("ello pookie", "Create new Shortcut", "Cancel", "Save") != ContentDialogResult.Primary)
        {
            return;
        }

        ShortcutManager.Save();
    }

    [RelayCommand]
    async Task RemoveShortcutAsync(
        Shortcut shortcut)
    {
        if (await windowHelper.AlertAsync("If you remove this shortcut, you wont be able to restore it again.", "Are you sure?", "Cancel", "Remove") != ContentDialogResult.Primary)
            return;

        ShortcutManager.Shortcuts.Remove(shortcut);
        ShortcutManager.Save();

        logger.LogInformation("[HomeViewModel-RemoveShortcut] Shortcut has been removed: {shortcutName}", shortcut.Name);
    }


    [RelayCommand]
    void NavigateToSettings() =>
        navigation.Navigate("Settings");
}