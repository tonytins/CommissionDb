using Avalonia;

namespace Commis.Desktop;

/// <summary>Starts the window.</summary>
public static class Program
{
    /// <summary>Runs Commis.</summary>
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Commis's Avalonia app, as the tests find it.</summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        // On X11, Avalonia leaves the title bar to the window manager unless it's asked to draw the frame itself,
        // which a bar that is also the title bar needs. The option is experimental in Avalonia 12, so it's on only
        // where the bar can merge.
#pragma warning disable AVALONIA_X11_CSD
        var x11 = new X11PlatformOptions { EnableDrawnDecorations = TitleBars.Merges() };
#pragma warning restore AVALONIA_X11_CSD
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(x11)
            .LogToTrace();
    }
}
