using System.Globalization;
using Jellyfin.Plugin.SendToKindle.Configuration;
using Jellyfin.Plugin.SendToKindle.Jobs;

namespace Jellyfin.Plugin.SendToKindle.Conversion;

public sealed class KccArgumentBuilder
{
    private readonly AdvancedArgumentParser _advancedArgumentParser;

    public KccArgumentBuilder(AdvancedArgumentParser advancedArgumentParser)
    {
        _advancedArgumentParser = advancedArgumentParser;
    }

    public IReadOnlyList<string> Build(
        BookSource source,
        string outputDirectory,
        PluginConfiguration configuration)
    {
        KccOptions options = configuration.Kcc ?? new KccOptions();
        List<string> arguments = new();

        AddValue(arguments, "--profile", options.Profile);
        AddFlag(arguments, "--manga-style", options.MangaStyle);
        AddFlag(arguments, "--hq", options.HighQuality);
        AddFlag(arguments, "--two-panel", options.TwoPanel);
        AddFlag(arguments, "--webtoon", options.Webtoon);
        AddFlag(arguments, "--upscale", options.Upscale);
        AddFlag(arguments, "--stretch", options.Stretch);
        AddValue(arguments, "--splitter", Math.Clamp(options.Splitter, 0, 2).ToString(CultureInfo.InvariantCulture));
        AddValue(arguments, "--gamma", string.IsNullOrWhiteSpace(options.Gamma) ? "Auto" : options.Gamma);
        AddValue(arguments, "--cropping", Math.Clamp(options.Cropping, 0, 2).ToString(CultureInfo.InvariantCulture));
        AddValue(
            arguments,
            "--croppingpower",
            Math.Clamp(options.CroppingPower, 0, 2).ToString("0.###", CultureInfo.InvariantCulture));
        AddFlag(arguments, "--autolevel", options.AutoLevel);
        AddFlag(arguments, "--noautocontrast", options.DisableAutoContrast);
        AddFlag(arguments, "--forcecolor", options.ForceColor);
        AddValue(
            arguments,
            "--jpeg-quality",
            Math.Clamp(options.JpegQuality, 0, 95).ToString(CultureInfo.InvariantCulture));
        AddFlag(arguments, "--spreadshift", options.SpreadShift);
        AddFlag(arguments, "--onepagelandscape", options.OnePageLandscape);

        arguments.AddRange(_advancedArgumentParser.Parse(options.AdvancedArguments));
        AddValue(arguments, "--format", "EPUB");
        arguments.Add("--nokepub");
        AddValue(arguments, "--batchsplit", "1");
        AddValue(
            arguments,
            "--targetsize",
            Math.Clamp(configuration.AttachmentLimitMegabytes, 1, 50).ToString(CultureInfo.InvariantCulture));
        AddValue(arguments, "--title", source.Title);
        AddValue(arguments, "--author", source.Author);
        AddValue(arguments, "--output", outputDirectory);
        arguments.Add(source.Path);
        return arguments;
    }

    private static void AddFlag(ICollection<string> arguments, string name, bool enabled)
    {
        if (enabled)
        {
            arguments.Add(name);
        }
    }

    private static void AddValue(ICollection<string> arguments, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        arguments.Add(name);
        arguments.Add(value);
    }
}
