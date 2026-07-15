namespace Jellyfin.Plugin.SendToKindle.Configuration;

public interface IPluginConfigurationAccessor
{
    PluginConfiguration Current { get; }
}
