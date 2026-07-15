using Jellyfin.Plugin.SendToKindle.Configuration;
using Jellyfin.Plugin.SendToKindle.Jobs;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Conversion;

public sealed class BookConversionService : IBookConversionService
{
    private static readonly HashSet<string> ComicExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cbr",
        ".cbz",
    };

    private static readonly HashSet<string> CalibreExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".mobi",
        ".azw",
        ".azw3",
    };

    private readonly IApplicationPaths _applicationPaths;
    private readonly IPluginConfigurationAccessor _configurationAccessor;
    private readonly IProcessRunner _processRunner;
    private readonly KccArgumentBuilder _kccArgumentBuilder;
    private readonly ILogger<BookConversionService> _logger;

    public BookConversionService(
        IApplicationPaths applicationPaths,
        IPluginConfigurationAccessor configurationAccessor,
        IProcessRunner processRunner,
        KccArgumentBuilder kccArgumentBuilder,
        ILogger<BookConversionService> logger)
    {
        _applicationPaths = applicationPaths;
        _configurationAccessor = configurationAccessor;
        _processRunner = processRunner;
        _kccArgumentBuilder = kccArgumentBuilder;
        _logger = logger;
    }

    public static bool IsSupportedExtension(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".epub", StringComparison.OrdinalIgnoreCase)
            || ComicExtensions.Contains(extension)
            || CalibreExtensions.Contains(extension);
    }

    public async Task<ConversionResult> ConvertAsync(BookSource source, CancellationToken cancellationToken)
    {
        PluginConfiguration configuration = _configurationAccessor.Current;

        ValidateConfiguration(configuration);
        if (!File.Exists(source.Path))
        {
            throw new FileNotFoundException("The Jellyfin book file no longer exists.", source.Path);
        }

        string workspace = CreateWorkspace(configuration);
        try
        {
            string extension = Path.GetExtension(source.Path);
            if (extension.Equals(".epub", StringComparison.OrdinalIgnoreCase))
            {
                string output = Path.Combine(workspace, CreateSafeFileName(source.Title) + ".epub");
                File.Copy(source.Path, output, overwrite: false);
            }
            else if (ComicExtensions.Contains(extension))
            {
                await ConvertComicAsync(source, workspace, configuration, cancellationToken).ConfigureAwait(false);
            }
            else if (CalibreExtensions.Contains(extension))
            {
                await ConvertWithCalibreAsync(source, workspace, configuration, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException($"Book format '{extension}' is not supported.");
            }

            IReadOnlyList<string> files = Directory
                .EnumerateFiles(workspace, "*.epub", SearchOption.TopDirectoryOnly)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (files.Count == 0)
            {
                throw new InvalidOperationException("The converter completed without producing an EPUB file.");
            }

            long limitBytes = configuration.AttachmentLimitMegabytes * 1_000_000L;
            foreach (string file in files)
            {
                long size = new FileInfo(file).Length;
                if (size == 0)
                {
                    throw new InvalidOperationException($"Converted output '{Path.GetFileName(file)}' is empty.");
                }

                if (size > limitBytes)
                {
                    throw new InvalidOperationException(
                        $"Converted output '{Path.GetFileName(file)}' is larger than the configured {configuration.AttachmentLimitMegabytes} MB limit.");
                }
            }

            return new ConversionResult(workspace, files);
        }
        catch
        {
            DeleteWorkspace(workspace);
            throw;
        }
    }

    private async Task ConvertComicAsync(
        BookSource source,
        string workspace,
        PluginConfiguration configuration,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> arguments = _kccArgumentBuilder.Build(source, workspace, configuration);
        ProcessResult result = await _processRunner.RunAsync(
            new ProcessRequest(
                configuration.KccExecutable,
                arguments,
                workspace,
                TimeSpan.FromMinutes(configuration.ConversionTimeoutMinutes)),
            cancellationToken).ConfigureAwait(false);
        EnsureSuccess("KCC", result);
    }

    private async Task ConvertWithCalibreAsync(
        BookSource source,
        string workspace,
        PluginConfiguration configuration,
        CancellationToken cancellationToken)
    {
        string output = Path.Combine(workspace, CreateSafeFileName(source.Title) + ".epub");
        string[] arguments =
        {
            source.Path,
            output,
            "--title",
            source.Title,
            "--authors",
            source.Author,
        };
        ProcessResult result = await _processRunner.RunAsync(
            new ProcessRequest(
                configuration.CalibreExecutable,
                arguments,
                workspace,
                TimeSpan.FromMinutes(configuration.ConversionTimeoutMinutes)),
            cancellationToken).ConfigureAwait(false);
        EnsureSuccess("Calibre", result);
    }

    private void EnsureSuccess(string converter, ProcessResult result)
    {
        if (result.ExitCode == 0)
        {
            _logger.LogDebug("{Converter} conversion completed: {Output}", converter, result.StandardOutput);
            return;
        }

        throw new InvalidOperationException(
            $"{converter} exited with code {result.ExitCode}: {LastUsefulText(result.StandardError, result.StandardOutput)}");
    }

    private string CreateWorkspace(PluginConfiguration configuration)
    {
        string root = string.IsNullOrWhiteSpace(configuration.TemporaryDirectory)
            ? Path.Combine(_applicationPaths.CachePath, "send-to-kindle")
            : Path.GetFullPath(configuration.TemporaryDirectory);
        Directory.CreateDirectory(root);
        string workspace = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static void ValidateConfiguration(PluginConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.KccExecutable))
        {
            throw new InvalidOperationException("The KCC executable is not configured.");
        }

        if (string.IsNullOrWhiteSpace(configuration.CalibreExecutable))
        {
            throw new InvalidOperationException("The Calibre executable is not configured.");
        }

        if (configuration.AttachmentLimitMegabytes is < 1 or > 50)
        {
            throw new InvalidOperationException("The attachment limit must be between 1 and 50 MB.");
        }

        if (configuration.ConversionTimeoutMinutes is < 1 or > 180)
        {
            throw new InvalidOperationException("The conversion timeout must be between 1 and 180 minutes.");
        }
    }

    private static string CreateSafeFileName(string title)
    {
        HashSet<char> invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        string safe = new(title.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
        safe = safe.Trim().TrimEnd('.');
        return string.IsNullOrEmpty(safe) ? "book" : safe;
    }

    private static string LastUsefulText(string primary, string fallback)
    {
        string text = string.IsNullOrWhiteSpace(primary) ? fallback : primary;
        text = text.Trim();
        return text.Length <= 2000 ? text : text[^2000..];
    }

    private static void DeleteWorkspace(string workspace)
    {
        try
        {
            Directory.Delete(workspace, recursive: true);
        }
        catch (IOException)
        {
            // A failed conversion should retain its original exception.
        }
        catch (UnauthorizedAccessException)
        {
            // A failed conversion should retain its original exception.
        }
    }
}
