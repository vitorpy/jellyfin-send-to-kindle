namespace Jellyfin.Plugin.SendToKindle.Conversion;

public sealed class AdvancedArgumentParser
{
    private static readonly IReadOnlyDictionary<string, int> SafeOptions = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["--noprocessing"] = 0,
        ["--legacyextract"] = 0,
        ["--pdfwidth"] = 0,
        ["--croppingminimum"] = 1,
        ["--preservemargin"] = 1,
        ["--interpanelcrop"] = 1,
        ["--blackborders"] = 0,
        ["--whiteborders"] = 0,
        ["--smartcovercrop"] = 0,
        ["--coverfill"] = 0,
        ["--forcepng"] = 0,
        ["--webp"] = 0,
        ["--force-png-rgb"] = 0,
        ["--pnglegacy"] = 0,
        ["--noquantize"] = 0,
        ["--mozjpeg"] = 0,
        ["--maximizestrips"] = 0,
        ["--norotate"] = 0,
        ["--rotateright"] = 0,
        ["--rotatefirst"] = 0,
        ["--filefusion"] = 0,
        ["--eraserainbow"] = 0,
        ["--customwidth"] = 1,
        ["--customheight"] = 1,
    };

    public IReadOnlyList<string> Parse(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return Array.Empty<string>();
        }

        IReadOnlyList<string> tokens = Tokenize(arguments);
        List<string> result = new(tokens.Count);

        for (int index = 0; index < tokens.Count; index++)
        {
            string token = tokens[index];
            string option = token;
            string? inlineValue = null;
            int equalsIndex = token.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex > 0)
            {
                option = token[..equalsIndex];
                inlineValue = token[(equalsIndex + 1)..];
            }

            if (!SafeOptions.TryGetValue(option, out int valueCount))
            {
                throw new ArgumentException($"KCC argument '{option}' is not in the safe advanced-argument allowlist.");
            }

            result.Add(option);
            if (valueCount == 0)
            {
                if (inlineValue is not null)
                {
                    throw new ArgumentException($"KCC argument '{option}' does not accept a value.");
                }

                continue;
            }

            string value;
            if (inlineValue is not null)
            {
                value = inlineValue;
            }
            else if (++index < tokens.Count)
            {
                value = tokens[index];
            }
            else
            {
                throw new ArgumentException($"KCC argument '{option}' requires a value.");
            }

            if (value.Length == 0 || value.StartsWith("-", StringComparison.Ordinal))
            {
                throw new ArgumentException($"KCC argument '{option}' has an invalid value.");
            }

            result.Add(value);
        }

        return result;
    }

    private static IReadOnlyList<string> Tokenize(string input)
    {
        List<string> tokens = new();
        List<char> current = new();
        char quote = '\0';
        bool escaping = false;

        foreach (char character in input)
        {
            if (escaping)
            {
                current.Add(character);
                escaping = false;
                continue;
            }

            if (character == '\\')
            {
                escaping = true;
                continue;
            }

            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
                else
                {
                    current.Add(character);
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quote = character;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                FlushToken(tokens, current);
                continue;
            }

            current.Add(character);
        }

        if (escaping || quote != '\0')
        {
            throw new ArgumentException("KCC advanced arguments contain an incomplete escape or quote.");
        }

        FlushToken(tokens, current);
        return tokens;
    }

    private static void FlushToken(ICollection<string> tokens, List<char> current)
    {
        if (current.Count == 0)
        {
            return;
        }

        tokens.Add(new string(current.ToArray()));
        current.Clear();
    }
}
