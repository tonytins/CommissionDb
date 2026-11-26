namespace Commis.Desktop;

/// <summary>Something the window can't do, in a sentence it can show as it is.</summary>
internal sealed class CommisException(string message, Exception? innerException = null)
    : Exception(message, innerException);
