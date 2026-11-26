using System.Globalization;

namespace Commis.Desktop.Records;

/// <summary>A record the window can hand to <c>commis add</c>, already read from the form.</summary>
/// <param name="Client">Who it's for.</param>
/// <param name="Reference">A link or other way to find the work.</param>
/// <param name="Description">What the work is, or empty.</param>
/// <param name="Fee">The amount charged.</param>
/// <param name="Title">The run's title for a YCH entry, or null.</param>
/// <param name="Slot">The slot for a YCH entry, or null for a commission.</param>
internal sealed record NewRecord(
    string Client,
    string Reference,
    string Description,
    decimal Fee,
    string? Title,
    int? Slot);

/// <summary>The form's fields, so a problem can point at one.</summary>
internal enum DraftField
{
    Client,
    Reference,
    Fee,
    Slot,
}

/// <summary>What the form's text amounts to: a record, or the first problem with it.</summary>
/// <param name="Record">The record, when every field reads.</param>
/// <param name="Problem">What's wrong, in the window's words, or null.</param>
/// <param name="Field">The field the problem is in.</param>
/// <param name="Missing">
/// Whether the problem is only that a required field is still blank, which the form doesn't call out while someone is
/// still typing.
/// </param>
internal sealed record DraftCheck(NewRecord? Record, string? Problem, DraftField? Field, bool Missing);

/// <summary>
/// Reads the add form. It checks only what the window must understand to build the command: that required fields are
/// there and numbers are numbers. Everything else, such as a fee below zero, is commis's to refuse, which the form
/// learns from a dry run.
/// </summary>
internal static class Draft
{
    internal static DraftCheck Check(
        Section section,
        string? client,
        string? reference,
        string? fee,
        string? slot,
        string? title,
        string? description,
        InterfaceText text)
    {
        client = client?.Trim() ?? "";
        reference = reference?.Trim() ?? "";
        fee = fee?.Trim() ?? "";
        slot = slot?.Trim() ?? "";

        if (client.Length == 0)
        {
            return Missing(DraftField.Client, TextKeys.FormNeedsClient, text);
        }

        if (reference.Length == 0)
        {
            return Missing(DraftField.Reference, TextKeys.FormNeedsReference, text);
        }

        if (fee.Length == 0)
        {
            return Missing(DraftField.Fee, TextKeys.FormNeedsFee, text);
        }

        if (!TryReadNumber(fee, out var amount))
        {
            return Wrong(DraftField.Fee, TextKeys.FormFeeNotNumber, text);
        }

        int? slotNumber = null;
        string? runTitle = null;
        if (section == Section.Ych)
        {
            // A slot is what makes an entry a YCH; without one, commis would quietly write a commission.
            if (slot.Length == 0)
            {
                return Missing(DraftField.Slot, TextKeys.FormNeedsSlot, text);
            }

            if (!int.TryParse(slot, NumberStyles.Integer, CultureInfo.CurrentCulture, out var number))
            {
                return Wrong(DraftField.Slot, TextKeys.FormSlotNotNumber, text);
            }

            slotNumber = number;
            runTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        }

        var record = new NewRecord(client, reference, description?.Trim() ?? "", amount, runTitle, slotNumber);
        return new DraftCheck(record, null, null, false);
    }

    // People type fees the way their language writes numbers; "25.50" also reads everywhere.
    private static bool TryReadNumber(string value, out decimal amount) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
        || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    private static DraftCheck Missing(DraftField field, string key, InterfaceText text) =>
        new(null, text.Get(key), field, true);

    private static DraftCheck Wrong(DraftField field, string key, InterfaceText text) =>
        new(null, text.Get(key), field, false);
}
