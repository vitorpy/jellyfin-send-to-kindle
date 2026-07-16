using Jellyfin.Plugin.SendToKindle.Configuration;
using Jellyfin.Plugin.SendToKindle.Controllers;
using Jellyfin.Plugin.SendToKindle.Conversion;

namespace Jellyfin.Plugin.SendToKindle.Diagnostics;

public sealed class ConverterDiagnosticService : IConverterDiagnosticService
{
    private readonly IProcessRunner _processRunner;
    private readonly IPluginConfigurationAccessor _configurationAccessor;

    public ConverterDiagnosticService(
        IProcessRunner processRunner,
        IPluginConfigurationAccessor configurationAccessor)
    {
        _processRunner = processRunner;
        _configurationAccessor = configurationAccessor;
    }

    public async Task<IReadOnlyList<DependencyCheckResult>> CheckAsync(CancellationToken cancellationToken)
    {
        PluginConfiguration configuration = _configurationAccessor.Current;
        return new[]
        {
            await CheckExecutableAsync(
                "KCC",
                configuration.KccExecutable,
                new[] { "--help" },
                cancellationToken).ConfigureAwait(false),
            await CheckExecutableAsync(
                "Calibre",
                configuration.CalibreExecutable,
                new[] { "--version" },
                cancellationToken).ConfigureAwait(false),
        };
    }

    private async Task<DependencyCheckResult> CheckExecutableAsync(
        string name,
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            return new DependencyCheckResult(name, false, "Executable is not configured.");
        }

        try
        {
            ProcessResult result = await _processRunner.RunAsync(
                new ProcessRequest(
                    executable,
                    arguments,
                    Path.GetTempPath(),
                    TimeSpan.FromSeconds(15)),
                cancellationToken).ConfigureAwait(false);
            string output = string.IsNullOrWhiteSpace(result.StandardOutput)
                ? result.StandardError.Trim()
                : result.StandardOutput.Trim();
            string message = string.IsNullOrWhiteSpace(output)
                ? $"Executable started and exited with code {result.ExitCode}."
                : output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return new DependencyCheckResult(name, result.ExitCode == 0, message);
        }
        catch (Exception exception)
        {
            return new DependencyCheckResult(name, false, exception.GetBaseException().Message);
        }
    }
}
