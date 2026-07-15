using System.Reflection;

namespace Jellyfin.Plugin.SendToKindle.WebIntegration;

public static class WebTransformCallback
{
    private const string Marker = "data-send-to-kindle-client";
    private static readonly Lazy<string> ClientScript = new(LoadClientScript);

    public static string Transform(WebTransformPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Contents.Contains(Marker, StringComparison.Ordinal))
        {
            return payload.Contents;
        }

        string script = $"<script {Marker}=\"true\">{ClientScript.Value}</script>";
        int bodyEnd = payload.Contents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return bodyEnd < 0
            ? payload.Contents + script
            : payload.Contents.Insert(bodyEnd, script);
    }

    private static string LoadClientScript()
    {
        Assembly assembly = typeof(WebTransformCallback).Assembly;
        string resourceName = $"{typeof(Plugin).Namespace}.Web.client.js";
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
