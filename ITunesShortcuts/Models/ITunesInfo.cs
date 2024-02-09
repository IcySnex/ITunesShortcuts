namespace ITunesShortcuts.Models;

public class ITunesInfo
{
    public ITunesInfo(
        string? version,
        string? installationLocation,
        DateOnly? installedAt)
    {
        this.Version = version;
        this.InstallationLocation = installationLocation;
        this.InstalledAt = installedAt;
    }


    public string? Version { get; }

    public string? InstallationLocation { get; }

    public DateOnly? InstalledAt { get; }
}