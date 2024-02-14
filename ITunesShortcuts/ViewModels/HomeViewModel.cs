using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITunesShortcuts.Enums;
using ITunesShortcuts.Models;
using ITunesShortcuts.Services;
using ITunesShortcuts.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace ITunesShortcuts.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    readonly ILogger<HomeViewModel> logger;
    readonly WindowHelper windowHelper;
    readonly Navigation navigation;
    readonly KeyboardListener keyboardListener;

    public ShortcutManager ShortcutManager { get; }

    public HomeViewModel(
        ILogger<HomeViewModel> logger,
        WindowHelper windowHelper,
        Navigation navigation,
        ShortcutManager shortcutManager,
        KeyboardListener keyboardListener)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;
        this.navigation = navigation;
        this.ShortcutManager = shortcutManager;
        this.keyboardListener = keyboardListener;

        logger.LogInformation("[HomeViewModel-.ctor] HomeViewModel has been initialized.");
    }


    [RelayCommand]
    async Task CreateShortcutAsync()
    {
        CreateShortcutViewModel viewModel = App.Provider.GetRequiredService<CreateShortcutViewModel>();
        CreateShortcutView view = new(viewModel);
        ContentDialog dialog = new()
        {
            Content = view,
            Title = "Create new Shortcut",
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Save"
        };
        dialog.SetBinding(ContentDialog.IsPrimaryButtonEnabledProperty, new Binding()
        {
            Source = viewModel,
            Path = new PropertyPath("IsValid"),
            Mode = BindingMode.OneWay
        });

        if (await windowHelper.AlertAsync(dialog) != ContentDialogResult.Primary)
            return;

        if (keyboardListener.GetKeys().Any(key => key.Key == viewModel.Key))
        {
            await windowHelper.AlertErrorAsync("The key is already used for another shortcut. Please choose another.", "Something went wrong!", "HomeViewModel-EditShortcutAsync");
            return;
        }

        Shortcut shortcut = new(
            viewModel.Name,
            viewModel.Key!.Value,
            viewModel.Modifiers,
            viewModel.Action,
            viewModel.Parameters[viewModel.SelectedParameter]);

        ShortcutManager.Shortcuts.Add(shortcut);
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
    async Task EditShortcutAsync(
        Shortcut shortcut)
    {
        CreateShortcutViewModel viewModel = App.Provider.GetRequiredService<CreateShortcutViewModel>();
        viewModel.Name = shortcut.Name;
        viewModel.Key = shortcut.Key;
        viewModel.Modifiers = shortcut.Modifiers ?? new();
        viewModel.Action = shortcut.Action;
        viewModel.SelectedParameter = Array.IndexOf(viewModel.Parameters, shortcut.Parameter);

        CreateShortcutView view = new(viewModel);
        ContentDialog dialog = new()
        {
            Content = view,
            Title = "Edit Shortcut",
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Save"
        };
        dialog.SetBinding(ContentDialog.IsPrimaryButtonEnabledProperty, new Binding()
        {
            Source = viewModel,
            Path = new PropertyPath("IsValid"),
            Mode = BindingMode.OneWay
        });

        if (await windowHelper.AlertAsync(dialog) != ContentDialogResult.Primary)
            return;

        if (shortcut.Key != viewModel.Key && keyboardListener.GetKeys().Any(key => key.Key == viewModel.Key))
        {
            await windowHelper.AlertErrorAsync("The key is already used for another shortcut. Please choose another.", "Something went wrong!", "HomeViewModel-EditShortcutAsync");
            return;
        }

        shortcut.Name = viewModel.Name;
        shortcut.Key = viewModel.Key!.Value;
        shortcut.Modifiers = viewModel.Modifiers;
        shortcut.Action = viewModel.Action;
        shortcut.Parameter = viewModel.Parameters[viewModel.SelectedParameter];

        ShortcutManager.Shortcuts.ForceRefresh();
    }


    [RelayCommand]
    void NavigateToSettings() =>
        navigation.Navigate("Settings");
}