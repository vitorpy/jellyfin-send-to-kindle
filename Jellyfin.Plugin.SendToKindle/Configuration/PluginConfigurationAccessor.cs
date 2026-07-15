namespace Jellyfin.Plugin.SendToKindle.Configuration;

public sealed class PluginConfigurationAccessor : IPluginConfigurationAccessor
{
    public PluginConfiguration Current => Plugin.Instance?.Configuration
        ?? throw new InvalidOperationException("The plugin configuration is not available.");
}
