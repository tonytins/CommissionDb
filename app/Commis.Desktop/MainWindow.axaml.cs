using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
    private readonly InterfaceText text;
    private readonly List<SectionItem> sectionItems;
    private readonly IReadOnlyList<FilterOption> filterOptions;
    private readonly IReadOnlyList<FilterOption> statusChoices;

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

    // The add form: open or not, its pending check, and which check is the latest.
    private readonly DispatcherTimer formCheck = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool formOpen;
    private int formChecks;

    /// <summary>Opens the window on the commis found for this computer.</summary>
    public MainWindow()
        : this(CommisLocation.Find(), new WindowSettings(WindowSettings.DefaultPath), InterfaceText.Load())
    {
    }

    /// <summary>Opens the window on the commis at <paramref name="commisPath"/>, or on none when it's null.</summary>
    internal MainWindow(string? commisPath, WindowSettings settings, InterfaceText text)
    {
        InitializeComponent();

        this.settings = settings;
        this.text = text;
        client = commisPath is null ? null : new CommisClient(commisPath, text);
        section = settings.LoadSection();
        sectionItems = Sections.All.Select(each => new SectionItem(each, text.Get(each.TextKey()))).ToList();
        filterOptions = FilterOption.Filters(text);
        statusChoices = FilterOption.Choices(text);
        ShowText();

        rendering = true;
        SectionList.ItemsSource = sectionItems;
        SectionList.SelectedIndex = Sections.All.ToList().IndexOf(section);
        FilterBox.ItemsSource = filterOptions;
        StatusBox.ItemsSource = statusChoices;
        rendering = false;

        MergeTitleBar(TitleBars.Merges());
        Render();

        YchList.ContainerPrepared += (_, e) =>
        {
            // A run's heading is a label, not a record: it can't be selected or reached with the arrow keys.
            var heading = e.Container.DataContext is YchHeader;
            e.Container.IsEnabled = !heading;
            e.Container.Focusable = !heading;
        };
        formCheck.Tick += async (_, _) =>
        {
            formCheck.Stop();
            await CheckFormAsync();
        };

        Opened += async (_, _) => await StartAsync();
        Closed += (_, _) => watcher?.Dispose();
    }

    /// <summary>Puts the interface text on everything that doesn't change while the window is open.</summary>
    private void ShowText()
    {
        Title = TitleText.Text = text.Get(TextKeys.AppTitle);
        NameButton(MinimizeButton, TextKeys.WindowMinimize);
        NameButton(MaximizeButton, TextKeys.WindowMaximize);
        NameButton(CloseButton, TextKeys.WindowClose);

        AddButton.Content = text.Get(TextKeys.ToolbarAdd);
        ExportButton.Content = text.Get(TextKeys.ToolbarExport);
        AutomationProperties.SetName(FilterBox, text.Get(TextKeys.ToolbarFilter));

        AutomationProperties.SetName(SectionList, text.Get(TextKeys.SidebarName));
        AutomationProperties.SetName(CommissionList, text.Get(TextKeys.ListName));
        AutomationProperties.SetName(YchList, text.Get(TextKeys.ListName));
        ClientHeader.Text = YchClientHeader.Text = text.Get(TextKeys.ColumnClient);
        TitleHeader.Text = text.Get(TextKeys.ColumnTitle);
        YchSlotHeader.Text = text.Get(TextKeys.ColumnSlot);
        FeeHeader.Text = YchFeeHeader.Text = text.Get(TextKeys.ColumnFee);
        StatusHeader.Text = YchStatusHeader.Text = text.Get(TextKeys.ColumnStatus);
        DateHeader.Text = YchDateHeader.Text = text.Get(TextKeys.ColumnDate);

        StatusLabel.Text = text.Get(TextKeys.DetailStatus);
        AutomationProperties.SetName(StatusBox, StatusLabel.Text);
        FeeLabel.Text = text.Get(TextKeys.DetailFee);
        TitleLabel.Text = text.Get(TextKeys.DetailTitle);
        DetailSlotLabel.Text = text.Get(TextKeys.DetailSlot);
        ReferenceLabel.Text = text.Get(TextKeys.DetailReference);
        DescriptionLabel.Text = text.Get(TextKeys.DetailDescription);
        AddedLabel.Text = text.Get(TextKeys.DetailAdded);

        RafflesTitle.Text = text.Get(TextKeys.RafflesTitle);
        RafflesBody.Text = text.Get(TextKeys.RafflesBody);
        UnavailableText.Text = text.Get(TextKeys.Unavailable);
        DismissButton.Content = text.Get(TextKeys.ProblemDismiss);

        Label(FormClientLabel, FormClient, TextKeys.FormClient);
        Label(FormReferenceLabel, FormReference, TextKeys.FormReference);
        Label(FormFeeLabel, FormFee, TextKeys.FormFee);
        Label(FormSlotLabel, FormSlot, TextKeys.FormSlot);
        Label(FormTitleLabel, FormTitle, TextKeys.FormTitle);
        Label(FormDescriptionLabel, FormDescription, TextKeys.FormDescription);
        FormTitle.PlaceholderText = FormDescription.PlaceholderText = text.Get(TextKeys.FormOptional);
        FormCancelButton.Content = text.Get(TextKeys.FormCancel);
        FormAddButton.Content = text.Get(TextKeys.FormAdd);
    }

    // A field's label, shown above it and given to screen readers.
    private void Label(TextBlock label, TextBox field, string key)
    {
        label.Text = text.Get(key);
        AutomationProperties.SetName(field, label.Text);
    }

    // A button that shows only a glyph is named for screen readers and tooltips alike.
    private void NameButton(Control control, string key)
    {
        var name = text.Get(key);
        AutomationProperties.SetName(control, name);
        ToolTip.SetTip(control, name);
    }

    private bool DrawsWindowButtons => merged && OperatingSystem.IsLinux() && WindowState != WindowState.FullScreen;

    private async Task StartAsync()
    {
        if (client is null)
        {
            ShowProblem(text.Get(TextKeys.ProblemNotFound, CommisLocation.Variable));
            return;
        }

        try
        {
            var info = await client.ReadInfoAsync();
            DatabaseText.Text = text.Get(TextKeys.DatabaseLine, info.Database, info.Schema, info.Commis);
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
        catch (CommisException exception)
        {
            // A refresh that a newer one replaced has nothing to say.
            if (refresh == refreshes)
            {
                ShowProblem(exception.Message);
            }
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

            FilterBox.IsEnabled = AddButton.IsEnabled = ExportButton.IsEnabled = loaded && showsRecords;
            var filter = filters.GetValueOrDefault(section);
            FilterBox.SelectedItem = filterOptions.First(option => option.Value == filter);

            // Commissions are rows; YCH entries stand in runs, each under its heading.
            var ych = section == Section.Ych;
            IReadOnlyList<RecordRow> rows;
            IReadOnlyList<object> items;
            if (ych)
            {
                var groups = Browser.Groups(records, filter, text);
                rows = groups.SelectMany(group => group.Rows).ToList();
                items = groups.SelectMany(group => group.Rows.Prepend<object>(group.Header)).ToList();
            }
            else
            {
                rows = Browser.Rows(records, section, filter, text);
                items = rows;
            }

            CommissionColumns.IsVisible = !ych;
            YchColumns.IsVisible = ych;
            var shown = ych ? YchList : CommissionList;
            var hidden = ych ? CommissionList : YchList;
            hidden.ItemsSource = null;
            hidden.IsVisible = false;
            shown.ItemsSource = items;
            shown.SelectedItem = rows.FirstOrDefault(row => row.Id == selectedId);
            shown.IsVisible = rows.Count > 0;
            EmptyText.Text = Browser.Empty(section, filter, text);
            EmptyText.IsVisible = rows.Count == 0;

            ShowDetail(SelectedRow);
        }
        finally
        {
            rendering = false;
        }
    }

    private ListBox CurrentList => section == Section.Ych ? YchList : CommissionList;

    private RecordRow? SelectedRow => CurrentList.SelectedItem as RecordRow;

    private void ShowDetail(RecordRow? row)
    {
        // The add form has the column while it's open.
        if (formOpen)
        {
            DetailView.IsVisible = NoSelectionText.IsVisible = false;
            return;
        }

        DetailView.IsVisible = row is not null;
        NoSelectionText.IsVisible = row is null;
        NoSelectionText.Text = text.Get(section == Section.Ych ? TextKeys.DetailNoneYch : TextKeys.DetailNoneCommission);
        if (row is null)
        {
            return;
        }

        DetailClient.Text = row.Client;
        StatusBox.SelectedItem = statusChoices.FirstOrDefault(option => option.Value == row.Record.Status);
        DetailFee.Text = row.Fee;
        DetailTitle.Text = row.Title;
        DetailSlot.Text = row.Slot;
        DetailSlotLabel.IsVisible = DetailSlot.IsVisible = section == Section.Ych;
        DetailReference.Text = row.Reference;
        DetailDescription.Text = row.Description;
        DetailDate.Text = row.DateAndTime;
    }

    private void ShowProblem(string message) => ShowMessage(message, problem: true);

    // Something done, such as an export, told in the same place as a problem but not in its colour.
    private void ShowNotice(string message) => ShowMessage(message, problem: false);

    private void ShowMessage(string message, bool problem)
    {
        ProblemText.Text = message;
        ProblemText.Classes.Set("problem", problem);
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

        CloseForm();
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
        if (rendering || sender != CurrentList)
        {
            return;
        }

        var row = SelectedRow;
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
            || SelectedRow is not { } row
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
                StatusBox.SelectedItem = statusChoices.FirstOrDefault(option => option.Value == row.Record.Status);
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

        // Ctrl+N (Cmd+N on macOS) adds a record where the section holds them.
        var command = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        if (e.Key == Key.N && e.KeyModifiers == command && AddButton.IsEnabled)
        {
            e.Handled = true;
            OpenForm();
            return;
        }

        // F5, or Ctrl+R (Cmd+R on macOS), reads the database again.
        if (e.Key == Key.F5 || (e.Key == Key.R && e.KeyModifiers == command))
        {
            e.Handled = true;
            await RefreshAsync();
        }
    }

    // Exporting

    /// <summary>
    /// Writes the section's records under the current filter to a CSV file the person picks. The file is chosen
    /// first, so cancelling runs nothing, and commis is asked only then, so the file holds what the database has at
    /// that moment.
    /// </summary>
    private async void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        if (client is null || section.RecordType() is not { } type)
        {
            return;
        }

        if (!StorageProvider.CanSave)
        {
            ShowProblem(text.Get(TextKeys.ExportNoPicker));
            return;
        }

        var status = filters.GetValueOrDefault(section);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = text.Get(TextKeys.ExportTitle),
            SuggestedFileName = Exports.FileName(section, status, text),
            DefaultExtension = "csv",
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType(text.Get(TextKeys.ExportFileType))
                {
                    Patterns = ["*.csv"],
                    MimeTypes = ["text/csv"],
                },
            ],
        });
        if (file is null)
        {
            return;
        }

        ExportButton.IsEnabled = false;
        var where = file.TryGetLocalPath() ?? file.Name;
        try
        {
            var csv = await client.ExportAsync(type, status);

            await using var stream = await file.OpenWriteAsync();
            if (stream.CanSeek)
            {
                // Replacing a longer file mustn't leave its tail behind.
                stream.SetLength(0);
            }

            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(csv);
            ShowNotice(text.Get(TextKeys.ExportSaved, where));
        }
        catch (CommisException exception)
        {
            ShowProblem(exception.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowProblem(text.Get(TextKeys.ExportFailed, where, exception.Message));
        }
        finally
        {
            file.Dispose();
            ExportButton.IsEnabled = true;
        }
    }

    // The add form

    private void OnAddClicked(object? sender, RoutedEventArgs e) => OpenForm();

    /// <summary>
    /// Opens the add form in the detail column, shaped by the section: a YCH entry asks for a slot and an optional
    /// title, which starts as the selected entry's run so the next slot in a run is quick to add.
    /// </summary>
    private void OpenForm()
    {
        if (section.RecordType() is null)
        {
            return;
        }

        var ych = section == Section.Ych;
        var run = ych ? SelectedRow?.Record.Title : null;
        formOpen = true;
        FormHeading.Text = text.Get(ych ? TextKeys.FormNewYch : TextKeys.FormNewCommission);
        FormYchFields.IsVisible = ych;
        foreach (var field in new[] { FormClient, FormReference, FormFee, FormSlot, FormDescription })
        {
            field.Text = string.Empty;
        }

        FormTitle.Text = run ?? string.Empty;
        FormProblem.IsVisible = false;
        FormAddButton.IsEnabled = true;
        AddForm.IsVisible = true;
        ShowDetail(null);
        Dispatcher.UIThread.Post(() => FormClient.Focus());
    }

    private void CloseForm()
    {
        if (!formOpen)
        {
            return;
        }

        formOpen = false;
        formCheck.Stop();
        formChecks++;
        AddForm.IsVisible = false;
        ShowDetail(SelectedRow);
    }

    private void OnFormCancel(object? sender, RoutedEventArgs e) => CloseForm();

    private void OnFormChanged(object? sender, TextChangedEventArgs e)
    {
        if (!formOpen)
        {
            return;
        }

        // Checked once typing pauses, not on every key.
        formCheck.Stop();
        formCheck.Start();
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseForm();
        }
        else if (e.Key == Key.Enter && e.Source != FormDescription && e.KeyModifiers == KeyModifiers.None)
        {
            // Enter adds from any one-line field; the description takes new lines.
            e.Handled = true;
            OnFormAdd(sender, e);
        }
    }

    private DraftCheck ReadForm() => Draft.Check(
        section, FormClient.Text, FormReference.Text, FormFee.Text, FormSlot.Text, FormTitle.Text,
        FormDescription.Text, text);

    /// <summary>
    /// Checks the form as it's filled in. The window's own checks come first; once they pass, a dry run asks commis,
    /// so anything it would refuse shows before Add is pressed. A blank required field isn't called out until then.
    /// </summary>
    private async Task CheckFormAsync()
    {
        var check = ReadForm();
        if (check.Record is null)
        {
            ShowFormProblem(check.Missing ? null : check.Problem);
            return;
        }

        if (client is null)
        {
            return;
        }

        var ticket = ++formChecks;
        try
        {
            await client.AddAsync(check.Record, dryRun: true);
            if (ticket == formChecks)
            {
                ShowFormProblem(null);
            }
        }
        catch (CommisException exception)
        {
            // A check that a newer one replaced has nothing to say.
            if (ticket == formChecks)
            {
                ShowFormProblem(exception.Message);
            }
        }
    }

    private async void OnFormAdd(object? sender, RoutedEventArgs e)
    {
        if (!formOpen || client is null || !FormAddButton.IsEnabled)
        {
            return;
        }

        var check = ReadForm();
        if (check.Record is not { } record)
        {
            ShowFormProblem(check.Problem);
            FieldFor(check.Field)?.Focus();
            return;
        }

        formCheck.Stop();
        formChecks++;
        FormAddButton.IsEnabled = false;
        try
        {
            await client.AddAsync(record, dryRun: false);
        }
        catch (CommisException exception)
        {
            // The form stays as it was, with commis's reason under it.
            ShowFormProblem(exception.Message);
            FormAddButton.IsEnabled = true;
            return;
        }

        var type = section.RecordType();
        CloseForm();
        await RefreshAsync();

        // Show what was just added: the newest record of the section's type.
        var added = records.Where(each => each.Type == type).Select(each => (int?)each.Id).Max();
        if (added is not null)
        {
            filters[section] = null;
            selectedId = added;
            Render();
        }
    }

    private void ShowFormProblem(string? problem)
    {
        FormProblem.Text = problem;
        FormProblem.IsVisible = problem is not null;
    }

    private TextBox? FieldFor(DraftField? field) => field switch
    {
        DraftField.Client => FormClient,
        DraftField.Reference => FormReference,
        DraftField.Fee => FormFee,
        DraftField.Slot => FormSlot,
        _ => null,
    };

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

        // macOS keeps its own buttons at the top left, so the toolbar starts clear of them. Only the toolbar moves,
        // so the title stays in the middle.
        if (merge && OperatingSystem.IsMacOS())
        {
            Toolbar.Margin = new Thickness(80, 0, 0, 0);
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
