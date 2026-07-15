using System.Diagnostics;
using System.Text;

namespace Jellyfin.Plugin.SendToKindle.Conversion;

public sealed class ProcessRunner : IProcessRunner
{
    private const int MaximumCapturedCharacters = 64 * 1024;

    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ProcessStartInfo startInfo = new()
        {
            FileName = request.FileName,
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (string argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Unable to start executable '{request.FileName}'.");
        }

        using CancellationTokenSource timeoutSource = new(request.Timeout);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        Task<string> stdoutTask = ReadBoundedAsync(process.StandardOutput, linkedSource.Token);
        Task<string> stderrTask = ReadBoundedAsync(process.StandardError, linkedSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillProcessTree(process);
            throw new TimeoutException(
                $"Executable '{request.FileName}' exceeded its {request.Timeout.TotalMinutes:0.#} minute timeout.");
        }
        catch (OperationCanceledException)
        {
            KillProcessTree(process);
            throw;
        }

        string[] output = await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
        return new ProcessResult(process.ExitCode, output[0], output[1]);
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        StringBuilder builder = new();
        char[] buffer = new char[4096];

        while (true)
        {
            int count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                break;
            }

            if (builder.Length < MaximumCapturedCharacters)
            {
                int remaining = MaximumCapturedCharacters - builder.Length;
                builder.Append(buffer, 0, Math.Min(count, remaining));
            }
        }

        return builder.ToString();
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the check and kill call.
        }
    }
}
