using Jellyfin.Plugin.SendToKindle.Configuration;
using Jellyfin.Plugin.SendToKindle.Controllers;
using Jellyfin.Plugin.SendToKindle.Conversion;
using Jellyfin.Plugin.SendToKindle.Diagnostics;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class ConverterDiagnosticServiceTests
{
    [Fact]
    public async Task CheckAsync_UsesSupportedProbeForEachConverter()
    {
        RecordingProcessRunner processRunner = new();
        ConverterDiagnosticService service = new(
            processRunner,
            new ConfigurationAccessor(new PluginConfiguration()));

        IReadOnlyList<DependencyCheckResult> results = await service.CheckAsync(CancellationToken.None);

        Assert.Equal(2, processRunner.Requests.Count);
        Assert.Equal("kcc-c2e", processRunner.Requests[0].FileName);
        Assert.Equal(new[] { "--help" }, processRunner.Requests[0].Arguments);
        Assert.Equal("ebook-convert", processRunner.Requests[1].FileName);
        Assert.Equal(new[] { "--version" }, processRunner.Requests[1].Arguments);
        Assert.All(results, result => Assert.True(result.Success));
    }

    private sealed class ConfigurationAccessor : IPluginConfigurationAccessor
    {
        public ConfigurationAccessor(PluginConfiguration configuration)
        {
            Current = configuration;
        }

        public PluginConfiguration Current { get; }
    }

    private sealed class RecordingProcessRunner : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = new();

        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            string output = request.FileName == "kcc-c2e"
                ? "comic2ebook v10.3.0 - Written by KCC contributors."
                : "ebook-convert (calibre 8.7.0)";
            return Task.FromResult(new ProcessResult(0, output, string.Empty));
        }
    }
}
