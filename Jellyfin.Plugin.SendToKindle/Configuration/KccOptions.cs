namespace Jellyfin.Plugin.SendToKindle.Configuration;

public sealed class KccOptions
{
    public string Profile { get; set; } = "KV";

    public bool MangaStyle { get; set; }

    public bool HighQuality { get; set; }

    public bool TwoPanel { get; set; }

    public bool Webtoon { get; set; }

    public bool Upscale { get; set; }

    public bool Stretch { get; set; }

    public int Splitter { get; set; }

    public string Gamma { get; set; } = string.Empty;

    public int Cropping { get; set; } = 2;

    public double CroppingPower { get; set; } = 1.0;

    public bool AutoLevel { get; set; }

    public bool DisableAutoContrast { get; set; }

    public bool ForceColor { get; set; }

    public int JpegQuality { get; set; } = 85;

    public bool SpreadShift { get; set; }

    public bool OnePageLandscape { get; set; }

    public string AdvancedArguments { get; set; } = string.Empty;
}
