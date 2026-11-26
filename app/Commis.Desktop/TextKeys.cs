namespace Commis.Desktop;

/// <summary>
/// The keys of the interface text in <c>uitext/&lt;language&gt;.dir/_100_commis.cst</c>. Every key here must be in the
/// English table, which a test checks.
/// </summary>
internal static class TextKeys
{
    // The window
    public const string AppTitle = "app title";
    public const string WindowMinimize = "window minimize";
    public const string WindowMaximize = "window maximize";
    public const string WindowClose = "window close";

    // The toolbar
    public const string ToolbarAdd = "toolbar add";
    public const string ToolbarExport = "toolbar export";
    public const string ToolbarExportLater = "toolbar export later";
    public const string ToolbarFilter = "toolbar filter";
    public const string FilterAll = "filter all";

    // Statuses
    public const string StatusPending = "status pending";
    public const string StatusPaid = "status paid";
    public const string StatusCancelled = "status cancelled";
    public const string StatusRefunded = "status refunded";

    // The sidebar
    public const string SidebarName = "sidebar name";
    public const string SectionCommissions = "section commissions";
    public const string SectionYch = "section ych";
    public const string SectionRaffles = "section raffles";

    // The record list
    public const string ListName = "list name";
    public const string ColumnClient = "column client";
    public const string ColumnTitle = "column title";
    public const string ColumnFee = "column fee";
    public const string ColumnStatus = "column status";
    public const string ColumnDate = "column date";
    public const string ColumnSlot = "column slot";
    public const string EmptyCommissions = "empty commissions";
    public const string EmptyCommissionsFiltered = "empty commissions filtered";
    public const string EmptyYch = "empty ych";
    public const string EmptyYchFiltered = "empty ych filtered";

    // YCH runs
    public const string YchUntitled = "ych untitled";
    public const string YchOneEntry = "ych one entry";
    public const string YchEntries = "ych entries";

    // The detail pane
    public const string DetailStatus = "detail status";
    public const string DetailFee = "detail fee";
    public const string DetailTitle = "detail title";
    public const string DetailSlot = "detail slot";
    public const string DetailReference = "detail reference";
    public const string DetailDescription = "detail description";
    public const string DetailAdded = "detail added";
    public const string DetailNoneCommission = "detail none commission";
    public const string DetailNoneYch = "detail none ych";

    // The add form
    public const string FormNewCommission = "form new commission";
    public const string FormNewYch = "form new ych";
    public const string FormClient = "form client";
    public const string FormReference = "form reference";
    public const string FormFee = "form fee";
    public const string FormSlot = "form slot";
    public const string FormTitle = "form title";
    public const string FormDescription = "form description";
    public const string FormOptional = "form optional";
    public const string FormAdd = "form add";
    public const string FormCancel = "form cancel";
    public const string FormNeedsClient = "form needs client";
    public const string FormNeedsReference = "form needs reference";
    public const string FormNeedsFee = "form needs fee";
    public const string FormNeedsSlot = "form needs slot";
    public const string FormFeeNotNumber = "form fee not number";
    public const string FormSlotNotNumber = "form slot not number";

    // Raffles
    public const string RafflesTitle = "raffles title";
    public const string RafflesBody = "raffles body";

    // The database and its problems
    public const string DatabaseLine = "database line";
    public const string Unavailable = "unavailable";
    public const string ProblemDismiss = "problem dismiss";
    public const string ProblemNotFound = "problem not found";
    public const string ProblemCannotStart = "problem cannot start";
    public const string ProblemTimeout = "problem timeout";
    public const string ProblemUnreadable = "problem unreadable";
    public const string ProblemExitCode = "problem exit code";

    /// <summary>The key for a status's name, such as <c>status paid</c> for <c>paid</c>.</summary>
    internal static string Status(string status) => "status " + status;
}
