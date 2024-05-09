using iTunesLib;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ITunesShortcuts.Services;

public class ITunesHelper : IDisposable
{
    readonly ILogger<ITunesHelper> logger;

    bool disposedValue;

    public ITunesHelper(
        ILogger<ITunesHelper> logger)
    {
        this.logger = logger;

        logger.LogInformation("[ITunesHelper-.ctor] ITunesHelper has been initialized.");
    }

    ~ITunesHelper()
    {
        Dispose(false);

        logger.LogInformation("[ITunesHelper-] ITunesHelper has been deinitialized.");
    }


    protected virtual void Dispose(
        bool explicitly)
    {
        if (disposedValue)
            return;

        if (explicitly)
        {
            registrySubKey = null;
        }

        iTunes.OnPlayerPlayEvent -= OnPlayerStart;
        iTunes.OnPlayerStopEvent -= OnPlayerStop;
        
        Marshal.ReleaseComObject(iTunes);
        iTunes = default!;

        GC.Collect();
        GC.WaitForPendingFinalizers();

        disposedValue = true;
        logger.LogInformation("[ITunesHelper-Dispose] ITunesHelper has been disposed [{explicitly}].", explicitly);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    iTunesApp iTunes = default!;
    string? registrySubKey = null;

    public ITunesInfo? GetInfo()
    {
        if (registrySubKey is null)
        {
            logger.LogError("[ITunesHelper-GetInfo] Failed to get iTunes info: registrySubKey is null.");
            return null;
        }

        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(registrySubKey);
        if (key is null)
        {
            logger.LogError("[ITunesHelper-GetInfo] Failed to get iTunes info: key is null.");
            return null;
        }

        string? version = key.GetValue("DisplayVersion")?.ToString();
        string? installLocation = key.GetValue("InstallLocation")?.ToString();
        string? installDate = key.GetValue("InstallDate")?.ToString();

        logger.LogInformation("[ITunesHelper-GetInfo] Got general iTunes information.");
        return new(version, installLocation, DateOnly.TryParseExact(installDate, "yyyyMMdd", out DateOnly date) ? date : null);
    }


    public bool IsInstalled()
    {
        logger.LogInformation("[ITunesHelper-IsInstalled] Checking registry for iTunes installation.");

        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

        if (key is null)
            return false;

        foreach (string subkeyName in key.GetSubKeyNames())
        {
            using RegistryKey? subkey = key.OpenSubKey(subkeyName);
            if (subkey is null)
                continue;

            if (subkey.GetValue("DisplayName") is object displayName && (displayName.ToString()?.Contains("iTunes") ?? false))
            {
                registrySubKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{subkeyName}";
                return true;
            }
        }
        return false;
    }

    public bool IsCOMRegistered()
    {
        logger.LogInformation("[ITunesHelper-IsCOMRegistered] Checking iTunes COM registration.");

        try
        {
            Type? type = Type.GetTypeFromProgID("iTunes.Application");
            return type is not null;
        }
        catch
        {
            return false;
        }
    }

    public bool IsInitialized()
    {
        logger.LogInformation("[ITunesHelper-IsInitialized] Checking iTunes initialization.");

        return iTunes is not null;
    }


    public void ValidateInstallation()
    {
        if (IsInstalled())
            return;

        int response = Win32.MessageBox(IntPtr.Zero, "ITunes installation was not found.\nThis application requires iTunes to be installed on the system.\nDo you want to ignore this error (may cause unexcepted crashes)?", "Error", Win32.MB_YESNO | Win32.MB_ICONERROR);
        logger.LogError("[ITunesHelper-ValidateInstallation] iTunes installation was not found: [{ignore}]", response == 7 ? "Exiting" : "Ignoring");

        if (response == 7)
        {
            Process.GetCurrentProcess().Kill();
            return;
        }
    }

    public void ValidateCOMRegistration()
    {
        if (IsCOMRegistered())
            return;

        int response = Win32.MessageBox(IntPtr.Zero, "ITunes COM registration failed.\nThis application requires iTunes COM to be registered.\nDo you want to ignore this error (may cause unexcepted crashes)?", "Error", Win32.MB_YESNO | Win32.MB_ICONERROR);
        logger.LogError("[ITunesHelper-ValidateCOMRegistration] iTunes COM registration failed: [{ignore}]", response == 7 ? "Exiting" : "Ignoring");

        if (response == 7)
        {
            Process.GetCurrentProcess().Kill();
            return;
        }
    }

    public void ValidateInitialization()
    {
        if (IsInitialized())
            return;

        iTunes = new();
        logger.LogInformation("[ITunesHelper-ValidateInitialization] iTunes was initialized and is ready.");
    }


    public IITFileOrCDTrack? GetCurrentTrack()
    {
        ValidateInitialization();

        if (iTunes.CurrentTrack is not IITFileOrCDTrack track)
            return null;

        logger.LogInformation("[ITunesHelper-GetCurrentTrack] Got current playing track.");
        return track;
    }

    public void PlayPause()
    {
        ValidateInitialization();

        iTunes.PlayPause();
    }
    public void BackTrack()
    {
        ValidateInitialization();

        iTunes.BackTrack();
    }
    public void NextTrack()
    {
        ValidateInitialization();

        iTunes.NextTrack();
    }

    public IEnumerable<IITUserPlaylist> GetAllPlaylists()
    {
        ValidateInitialization();

        IEnumerable<IITUserPlaylist> playlists = iTunes.LibrarySource.Playlists.OfType<IITUserPlaylist>().Where(playlist => playlist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone && !playlist.Smart);

        logger.LogInformation("[ITunesHelper-GetAllPlaylists] Got all library playlists.");
        return playlists;
    }
}