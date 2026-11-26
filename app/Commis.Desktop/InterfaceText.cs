using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CSTNet;

namespace Commis.Desktop;

/// <summary>
/// The interface text, from <c>.cst</c> tables. English is the base, and the person's language is laid over it, so a
/// string a translation lacks shows in English. Adapted from S1PK's.
/// </summary>
internal sealed class InterfaceText
{
    /// <summary>The table that holds Commis's text, <c>_100_commis.cst</c>.</summary>
    internal const int TableId = 100;

    private const string BaseLanguage = "english";

    private readonly CSTLayers layers;

    /// <summary>Reads the tables in <paramref name="uitextFolder"/> for <paramref name="culture"/>.</summary>
    /// <param name="uitextFolder">The folder holding one <c>&lt;language&gt;.dir</c> folder per language.</param>
    /// <param name="culture">The person's interface culture.</param>
    internal InterfaceText(string uitextFolder, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(uitextFolder);
        ArgumentNullException.ThrowIfNull(culture);

        Language = LanguageName(culture);
        var english = Read(Path.Combine(uitextFolder, BaseLanguage + ".dir"));

        var translationFolder = Path.Combine(uitextFolder, Language + ".dir");
        layers = Language != BaseLanguage && Directory.Exists(translationFolder)
            ? new CSTLayers(Read(translationFolder), english)
            : new CSTLayers(english);
    }

    /// <summary>The language asked for, such as <c>french</c>, whether or not a translation exists.</summary>
    internal string Language { get; }

    /// <summary>Reads the text shipped beside the app, for the person's interface culture.</summary>
    internal static InterfaceText Load() =>
        new(Path.Combine(AppContext.BaseDirectory, "uitext"), CultureInfo.CurrentUICulture);

    /// <summary>
    /// Gets a string, filling its placeholders, such as <c>%s</c>, with <paramref name="variables"/>. A string missing
    /// from every table shows its key, so the gap is visible rather than blank.
    /// </summary>
    /// <param name="key">A key from <see cref="TextKeys"/>.</param>
    /// <param name="variables">Values for the string's placeholders, in order.</param>
    internal string Get(string key, params object?[] variables)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGet(key, out var value))
        {
            return key;
        }

        return variables.Length == 0 ? value : CST.Format(value, variables);
    }

    /// <summary>Whether any table has a non-empty string for <paramref name="key"/>.</summary>
    internal bool Has(string key) => TryGet(key, out _);

    /// <summary>The folder name a culture's tables use: the language's English name in lower case, such as <c>french</c>.</summary>
    internal static string LanguageName(CultureInfo culture)
    {
        if (string.IsNullOrEmpty(culture.Name))
        {
            return BaseLanguage;
        }

        var language = culture.IsNeutralCulture ? culture : culture.Parent;
        return language.EnglishName.ToLowerInvariant();
    }

    private bool TryGet(string key, [NotNullWhen(true)] out string? value) =>
        layers.TryGetValue(TableId, key, out value, treatEmptyAsMissing: true);

    private static CSTDirectory Read(string folder) =>
        Directory.Exists(folder) ? CSTDirectory.Read(folder) : new CSTDirectory(new Dictionary<int, CSTTable>());
}
