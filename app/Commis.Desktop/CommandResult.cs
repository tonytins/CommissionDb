namespace Commis.Desktop;

/// <summary>What a run of <c>commis</c> said.</summary>
/// <param name="ExitCode">Its exit code.</param>
/// <param name="StandardOutput">Everything it wrote to standard output.</param>
/// <param name="StandardError">Everything it wrote to standard error.</param>
internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
