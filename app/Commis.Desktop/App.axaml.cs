using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Commis.Desktop.Theme;

namespace Commis.Desktop;

/// <summary>Commis's Avalonia app.</summary>
public sealed partial class App : Application
{
    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        ApplyLook();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Gives every role a value on both palettes, and hands Fluent the same values so its controls agree with them.
    /// The values live in <see cref="Palette"/> so the contrast test checks what the window actually draws.
    /// </summary>
    private void ApplyLook()
    {
        var fluent = Styles.OfType<FluentTheme>().First();
        fluent.Palettes[ThemeVariant.Light] = Palette.Light.ToFluent();
        fluent.Palettes[ThemeVariant.Dark] = Palette.Dark.ToFluent();

        var roles = new ResourceDictionary
        {
            ThemeDictionaries =
            {
                [ThemeVariant.Light] = Palette.Light.ToResources(),
                [ThemeVariant.Dark] = Palette.Dark.ToResources(),
            },
        };
        Resources.MergedDictionaries.Add(roles);
    }
}
