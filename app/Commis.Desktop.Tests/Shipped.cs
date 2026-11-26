using System.Globalization;

namespace Commis.Desktop.Tests;

/// <summary>The interface text as it ships, in English, so tests check the sentences people see.</summary>
internal static class Shipped
{
    internal static string Folder => Path.Combine(AppContext.BaseDirectory, "uitext");

    internal static InterfaceText English { get; } = new(Folder, CultureInfo.InvariantCulture);
}
