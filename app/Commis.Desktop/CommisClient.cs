using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Commis.Desktop;

/// <summary>
/// Runs the <c>commis</c> executable and reads what it says. Every read and every change goes through it, so the
/// window never opens the database itself and there is one implementation of the schema, the migrations, and the
/// validation.
/// </summary>
/// <param name="executable">The path to the executable.</param>
/// <param name="text">The interface text its problems are told in.</param>
internal sealed class CommisClient(string executable, InterfaceText text)
{
    /// <summary>How long a command may take before the window gives up on it.</summary>
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    /// <summary>The program the window runs.</summary>
    internal string Executable { get; } = executable;

    /// <summary>Asks where the database is and what it holds.</summary>
    /// <exception cref="CommisException">The executable couldn't be run, or its answer couldn't be read.</exception>
    internal async Task<Info> ReadInfoAsync(CancellationToken cancellationToken = default) =>
        Read<Info>(await RunAsync(["info", "--json"], cancellationToken));

    /// <summary>Asks for the records, optionally narrowed by status or type.</summary>
    /// <exception cref="CommisException">The executable couldn't be run, or its answer couldn't be read.</exception>
    internal async Task<IReadOnlyList<Record>> ListRecordsAsync(
        string? status = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        List<string> arguments = ["list", "--json"];
        if (!string.IsNullOrEmpty(status))
        {
            arguments.Add("--status");
            arguments.Add(status);
        }

        if (!string.IsNullOrEmpty(type))
        {
            arguments.Add("--type");
            arguments.Add(type);
        }

        return Read<RecordsEnvelope>(await RunAsync(arguments, cancellationToken)).Records;
    }

    /// <summary>Sets a record's status with <c>commis status</c>.</summary>
    /// <exception cref="CommisException">commis couldn't be run, or refused, in its own words.</exception>
    internal async Task SetStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(
            ["status", id.ToString(System.Globalization.CultureInfo.InvariantCulture), "--set", status],
            cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new CommisException(Explain(result, text));
        }
    }

    /// <summary>
    /// The sentence to show for a run that produced no answer. What the command wrote on standard error comes first,
    /// then standard output when it isn't JSON, then the exit code.
    /// </summary>
    internal static string Explain(CommandResult result, InterfaceText text)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            return result.StandardError.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.StandardOutput) && !LooksLikeJson(result.StandardOutput))
        {
            return result.StandardOutput.Trim();
        }

        return result.ExitCode == 0
            ? text.Get(TextKeys.ProblemUnreadable)
            : text.Get(TextKeys.ProblemExitCode, result.ExitCode);
    }

    // The answer wins over the exit code: a command that wrote the contract is a success whatever it returned.
    private T Read<T>(CommandResult result) where T : class
    {
        if (TryRead<T>(result.StandardOutput, out var value))
        {
            return value;
        }

        throw new CommisException(Explain(result, text));
    }

    private static bool TryRead<T>(string text, out T value) where T : class
    {
        value = null!;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            var read = JsonSerializer.Deserialize<T>(text, Contract.Json);
            if (read is null)
            {
                return false;
            }

            value = read;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool LooksLikeJson(string text)
    {
        var trimmed = text.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private async Task<CommandResult> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        ProcessStartInfo start = new(Executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new CommisException(text.Get(TextKeys.ProblemCannotStart, Executable, Reason(exception)), exception);
        }

        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errors = process.StandardError.ReadToEndAsync(cancellationToken);

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Timeout);
        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Stop(process);
            throw new CommisException(text.Get(TextKeys.ProblemTimeout, (int)Timeout.TotalSeconds));
        }

        return new CommandResult(process.ExitCode, await output, await errors);
    }

    // The system's own reason, such as "No such file or directory", without .NET's sentence wrapped around it.
    private static string Reason(Exception exception) =>
        exception is Win32Exception system ? new Win32Exception(system.NativeErrorCode).Message : exception.Message;

    private static void Stop(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // It ended on its own between the check and the kill.
        }
    }
}
