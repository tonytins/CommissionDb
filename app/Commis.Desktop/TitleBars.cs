namespace Commis.Desktop;

/// <summary>
/// Whether Commis's bar can be the window's title bar, or should sit under the title bar the window manager draws.
/// Adapted from VentureOS's Roundhouse, by way of Depot and S1PK: Windows and macOS always can; on Linux, a desktop
/// that names itself can, except a tiling window manager, which places windows itself, and WSL, whose windows go
/// through another system's frames.
/// </summary>
internal static class TitleBars
{
    // Window managers that tile, by the names they give XDG_CURRENT_DESKTOP, in lower case.
    private static readonly string[] Tiling =
    [
        "awesome", "bspwm", "cagebreak", "dwl", "dwm", "herbstluftwm", "hyprland", "i3", "leftwm", "niri", "qtile",
        "river", "sway", "xmonad",
    ];

    /// <summary>Whether the bar is the title bar on this computer.</summary>
    internal static bool Merges() =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        || Merges(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"), Environment.GetEnvironmentVariable("WSL_DISTRO_NAME"));

    /// <summary>
    /// Whether the bar is the title bar on a Linux desktop named <paramref name="desktop"/>, inside WSL when
    /// <paramref name="wsl"/> is set.
    /// </summary>
    internal static bool Merges(string? desktop, string? wsl)
    {
        if (!string.IsNullOrEmpty(wsl) || string.IsNullOrEmpty(desktop))
        {
            return false;
        }

        // XDG_CURRENT_DESKTOP can list several names, such as "KDE" or "ubuntu:GNOME".
        foreach (var name in desktop.Split(':'))
        {
            if (Tiling.Contains(name.Trim().ToLowerInvariant()))
            {
                return false;
            }
        }

        return true;
    }
}
