using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Commis.Desktop.Records;

namespace Commis.Desktop;

/// <summary>
/// Commis's window: the places on the left, their records on the right. It reads and changes nothing itself; every
/// command goes through the <c>commis</c> executable, and every refresh is one <c>commis list --json</c>.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly CommisClient? client;
    private readonly WindowSettings settings;
    private readonly List<SectionItem> sectionItems = Sections.All.Select(section => new SectionItem(section)).ToList();

    // The filter belongs to the section, lasts while the window is open, and starts at all statuses.
    private readonly Dictionary<Section, string?> filters = [];

    private DatabaseWatcher? watcher;
    private IReadOnlyList<Record> records = [];
    private Section section;
    private int? selectedId;
    private int refreshes;
    private bool loaded;

    // Set while the window changes a selection itself, so the handlers don't take it for the person's choice.
    private bool rendering;
    private bool merged;

    /// <summary>Opens the window on the commis found for this computer.</summary>
    public MainWindow() : this(CommisLocation.Find(), new WindowSettings(WindowSettings.DefaultPath))
    {
    }

    /// <summary>Opens the window on the commis at <paramref name="commisPath"/>, or on none when it's null.</summary>
    internal MainWindow(string? commisPath, WindowSettings settings)
    {
        InitializeComponent();

        this.settings = settings;
        client = commisPath is null ? null : new CommisClient(commisPath);
        section = settings.LoadSection();

        rendering = true;
        SectionList.ItemsSource = sectionItems;
        SectionList.SelectedIndex = Sections.All.ToList().IndexOf(section);
        FilterBox.ItemsSource = FilterOption.Filters;
        StatusBox.ItemsSource = FilterOption.Choices;
        rendering = false;

        MergeTitleBar(TitleBars.Merges());
        Render();

        Opened += async (_, _) => await StartAsync();
        Closed += (_, _) => watcher?.Dispose();
    }

    private bool DrawsWindowButtons => merged && OperatingSystem.IsLinux() && WindowState != WindowState.FullScreen;

    private async Task StartAsync()
    {
        if (client is null)
        {
            ShowProblem($"Couldn't find commis. Set {CommisLocation.Variable} to its path, or put it on the PATH.");
            return;
        }

        try
        {
            var info = await client.ReadInfoAsync();
            DatabaseText.Text = $"{info.Database} · schema {info.Schema} · commis {info.Commis}";
            ToolTip.SetTip(DatabaseText, info.Database);
            watcher = new DatabaseWatcher(info.Database, () => Dispatcher.UIThread.Post(() => _ = RefreshAsync()));
        }
        catch (CommisException exception)
        {
            ShowProblem(exception.Message);
            return;
        }

        await RefreshAsync();
    }

    /// <summary>
    /// Reads every record again and redraws. A failure keeps what was showing and says why; an older refresh that
    /// finishes after a newer one is dropped.
    /// </summary>
    private async Task RefreshAsync()
    {
        if (client is null)
        {
            return;
        }

        var refresh = ++refreshes;

        // Taken before reading, so a write that lands while commis runs still counts as new.
        watcher?.MarkSeen();
        try
        {
            var read = await client.ListRecordsAsync();
            if (refresh != refreshes)
            {
                return;
            }

            records = read;
            loaded = true;
            Render();
        }
        catch (CommisException exception) when (refresh == refreshes)
        {
            ShowProblem(exception.Message);
        }
    }

    private void Render()
    {
        rendering = true;
        try
        {
            foreach (var item in sectionItems)
            {
                item.Count = loaded && Browser.Count(records, item.Section) is { } count ? count.ToString() : "";
            }

            var showsRecords = section.RecordType() is not null;
            RecordsView.IsVisible = loaded && showsRecords;
            RafflesView.IsVisible = section == Section.Raffles;
            UnavailableText.IsVisible = !loaded && showsRecords;

            FilterBox.IsEnabled = loaded && showsRecords;
            var filter = filters.GetValueOrDefault(section);
            FilterBox.SelectedItem = FilterOption.Filters.First(option => option.Value == filter);

            var rows = Browser.Rows(records, section, filter);
            RecordList.ItemsSource = rows;
            RecordList.SelectedItem = rows.FirstOrDefault(row => row.Id == selectedId);
            EmptyText.Text = Browser.Empty(section, filter);
            EmptyText.IsVisible = rows.Count == 0;
            RecordList.IsVisible = rows.Count > 0;

            ShowDetail(RecordList.SelectedItem as RecordRow);
        }
        finally
        {
            rendering = false;
        }
    }

    private void ShowDetail(RecordRow? row)
    {
        DetailView.IsVisible = row is not null;
        NoSelectionText.IsVisible = row is null;
        NoSelectionText.Text = section == Section.Ych
            ? "Select a YCH entry to see it here."
            : "Select a commission to see it here.";
        if (row is null)
        {
            return;
        }

        DetailClient.Text = row.Client;
        StatusBox.SelectedItem = FilterOption.Choices.FirstOrDefault(option => option.Value == row.Record.Status);
        DetailFee.Text = row.Fee;
        DetailTitle.Text = row.Title;
        DetailSlot.Text = row.Slot;
        DetailSlotLabel.IsVisible = DetailSlot.IsVisible = section == Section.Ych;
        DetailReference.Text = row.Reference;
        DetailDescription.Text = row.Description;
        DetailDate.Text = row.DateAndTime;
    }

    private void ShowProblem(string message)
    {
        ProblemText.Text = message;
        ProblemBar.IsVisible = true;
    }

    private void HideProblem() => ProblemBar.IsVisible = false;

    // What the person does

    private void OnSectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (rendering || SectionList.SelectedItem is not SectionItem item || item.Section == section)
        {
            return;
        }

        section = item.Section;
        selectedId = null;
        settings.SaveSection(section);
        Render();
    }

    private void OnFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (rendering || FilterBox.SelectedItem is not FilterOption option)
        {
            return;
        }

        filters[section] = option.Value;
        Render();
    }

    private void OnRecordChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (rendering)
        {
            return;
        }

        var row = RecordList.SelectedItem as RecordRow;
        selectedId = row?.Id;
        rendering = true;
        try
        {
            ShowDetail(row);
        }
        finally
        {
            rendering = false;
        }
    }

    private async void OnStatusChosen(object? sender, SelectionChangedEventArgs e)
    {
        if (rendering || client is null
            || RecordList.SelectedItem is not RecordRow row
            || StatusBox.SelectedItem is not FilterOption { Value: { } status }
            || status == row.Record.Status)
        {
            return;
        }

        StatusBox.IsEnabled = false;
        try
        {
            await client.SetStatusAsync(row.Id, status);
            HideProblem();
            await RefreshAsync();
        }
        catch (CommisException exception)
        {
            // The row stays as it was: put the box back and say why, in commis's words.
            ShowProblem(exception.Message);
            rendering = true;
            try
            {
                StatusBox.SelectedItem = FilterOption.Choices.FirstOrDefault(option => option.Value == row.Record.Status);
            }
            finally
            {
                rendering = false;
            }
        }
        finally
        {
            StatusBox.IsEnabled = true;
        }
    }

    private void OnDismissProblem(object? sender, RoutedEventArgs e) => HideProblem();

    /// <inheritdoc/>
    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        // F5, or Ctrl+R (Cmd+R on macOS), reads the database again.
        var command = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        if (e.Key == Key.F5 || (e.Key == Key.R && e.KeyModifiers == command))
        {
            e.Handled = true;
            await RefreshAsync();
        }
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
        ExtendClientAreaTitleBarHeightHint = merge ? TitleBar.Height : -1;

        // The bar drags the window; its buttons and controls take clicks.
        WindowDecorationProperties.SetElementRole(
            TitleBar, merge ? WindowDecorationsElementRole.TitleBar : WindowDecorationsElementRole.None);
        Control[] interactive = [.. WindowButtons.Children.OfType<Button>(), AddButton, FilterBox, ExportButton];
        foreach (var control in interactive)
        {
            WindowDecorationProperties.SetElementRole(
                control, merge ? WindowDecorationsElementRole.User : WindowDecorationsElementRole.None);
        }

        if (merge && OperatingSystem.IsLinux())
        {
            WindowDecorationsTheme = (ControlTheme)Resources["FrameOnly"]!;
        }

        // Where the window manager draws the title bar, it already names the window.
        TitleText.IsVisible = merge;

        // macOS keeps its own buttons at the top left, so the toolbar starts clear of them.
        if (merge && OperatingSystem.IsMacOS())
        {
            TitleBar.Padding = new Thickness(72, 0, 0, 0);
        }

        ShowWindowButtons();
    }

    private void ShowWindowButtons() => WindowButtons.IsVisible = DrawsWindowButtons;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty && WindowButtons is not null)
        {
            ShowWindowButtons();
        }
    }

    private void OnMinimizeClicked(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClicked(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Double-clicking the bar itself, not a control on it, maximizes or restores, as a title bar does.
        if (merged && e.Source is Visual source
            && source.FindAncestorOfType<Button>(includeSelf: true) is null
            && source.FindAncestorOfType<ComboBox>(includeSelf: true) is null)
        {
            OnMaximizeClicked(sender, e);
        }
    }
}
