namespace Jellyfin.Plugin.SendToKindle.Conversion;

public sealed class ConversionResult : IAsyncDisposable
{
    public ConversionResult(string workspacePath, IReadOnlyList<string> files)
    {
        WorkspacePath = workspacePath;
        Files = files;
    }

    public string WorkspacePath { get; }

    public IReadOnlyList<string> Files { get; }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (Directory.Exists(WorkspacePath))
            {
                Directory.Delete(WorkspacePath, recursive: true);
            }
        }
        catch (IOException)
        {
            // Cleanup is best effort; stale workspaces remain inside the configured cache directory.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup is best effort; the conversion result has already been consumed.
        }

        return ValueTask.CompletedTask;
    }
}
