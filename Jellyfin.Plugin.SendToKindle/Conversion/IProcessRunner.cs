namespace Jellyfin.Plugin.SendToKindle.Conversion;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);
}
