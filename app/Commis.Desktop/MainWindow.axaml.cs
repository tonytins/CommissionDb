using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Commis.Desktop;

/// <summary>
/// Commis's window: the places on the left, their records on the right. It reads and changes nothing itself; every
/// command goes through the <c>commis</c> executable.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly CommisClient? client;
    private readonly Border titleBar;
    private readonly TextBlock titleText;
    private readonly StackPanel windowButtons;
    private readonly Button maximizeButton;
    private readonly TextBlock summaryText;
    private readonly TextBlock detailText;
    private readonly TextBlock problemText;
    private readonly ListBox sectionList;
    private bool merged;

    /// <summary>Opens the window on the commis found for this computer.</summary>
    public MainWindow() : this(CommisLocation.Find())
    {
    }

    /// <summary>Opens the window on the commis at <paramref name="commisPath"/>, or on none when it's null.</summary>
    internal MainWindow(string? commisPath)
    {
        AvaloniaXamlLoader.Load(this);

        titleBar = this.Get<Border>("TitleBar");
        titleText = this.Get<TextBlock>("TitleText");
        windowButtons = this.Get<StackPanel>("WindowButtons");
        maximizeButton = this.Get<Button>("MaximizeButton");
        summaryText = this.Get<TextBlock>("SummaryText");
        detailText = this.Get<TextBlock>("DetailText");
        problemText = this.Get<TextBlock>("ProblemText");
        sectionList = this.Get<ListBox>("SectionList");

        sectionList.ItemsSource = new[] { "Commissions", "YCH", "Raffles" };
        sectionList.SelectedIndex = 0;

        client = commisPath is null ? null : new CommisClient(commisPath);
        MergeTitleBar(TitleBars.Merges());

        Opened += async (_, _) => await ShowDatabaseAsync();
    }

    private bool DrawsWindowButtons => merged && OperatingSystem.IsLinux() && WindowState != WindowState.FullScreen;

    private async Task ShowDatabaseAsync()
    {
        if (client is null)
        {
            ShowProblem($"Couldn't find commis. Set {CommisLocation.Variable} to its path, or put it on the PATH.");
            return;
        }

        try
        {
            var info = await client.ReadInfoAsync();
            summaryText.Text = info.Records == 1 ? "1 record" : $"{info.Records} records";
            detailText.Text = $"{info.Database} · schema {info.Schema} · commis {info.Commis}";
            summaryText.IsVisible = detailText.IsVisible = true;
            problemText.IsVisible = false;
        }
        catch (CommisException exception)
        {
            ShowProblem(exception.Message);
        }
    }

    private void ShowProblem(string message)
    {
        summaryText.IsVisible = detailText.IsVisible = false;
        problemText.Text = message;
        problemText.IsVisible = true;
    }

    // The bar, adapted from Roundhouse

    /// <summary>
    /// Makes the bar the window's title bar where the desktop allows it. On Linux, Avalonia draws the frame and
    /// Commis the buttons; Windows and macOS keep their own buttons over the bar. Elsewhere the window manager draws
    /// its title bar and the bar sits under it, still holding the toolbar.
    /// </summary>
    private void MergeTitleBar(bool merge)
    {
        merged = merge;
        ExtendClientAreaToDecorationsHint = merge;
        ExtendClientAreaTitleBarHeightHint = merge ? titleBar.Height : -1;

        // The bar drags the window; its buttons take clicks.
        WindowDecorationProperties.SetElementRole(
            titleBar, merge ? WindowDecorationsElementRole.TitleBar : WindowDecorationsElementRole.None);
        foreach (var button in windowButtons.Children.OfType<Button>())
        {
            WindowDecorationProperties.SetElementRole(
                button, merge ? WindowDecorationsElementRole.User : WindowDecorationsElementRole.None);
        }

        if (merge && OperatingSystem.IsLinux())
        {
            WindowDecorationsTheme = (ControlTheme)Resources["FrameOnly"]!;
        }

        // Where the window manager draws the title bar, it already names the window.
        titleText.IsVisible = merge;

        // macOS keeps its own buttons at the top left, so the toolbar starts clear of them.
        if (merge && OperatingSystem.IsMacOS())
        {
            titleBar.Padding = new Thickness(72, 0, 0, 0);
        }

        ShowWindowButtons();
    }

    private void ShowWindowButtons() => windowButtons.IsVisible = DrawsWindowButtons;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty && maximizeButton is not null)
        {
            ShowWindowButtons();
        }
    }

    private void OnMinimizeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Double-clicking the bar itself, not a button on it, maximizes or restores, as a title bar does.
        if (merged && e.Source is Visual source && source.FindAncestorOfType<Button>(includeSelf: true) is null)
        {
            OnMaximizeClicked(sender, e);
        }
    }
}
