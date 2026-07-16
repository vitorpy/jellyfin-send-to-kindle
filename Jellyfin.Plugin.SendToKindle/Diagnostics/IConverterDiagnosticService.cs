using Jellyfin.Plugin.SendToKindle.Controllers;

namespace Jellyfin.Plugin.SendToKindle.Diagnostics;

public interface IConverterDiagnosticService
{
    Task<IReadOnlyList<DependencyCheckResult>> CheckAsync(CancellationToken cancellationToken);
}
