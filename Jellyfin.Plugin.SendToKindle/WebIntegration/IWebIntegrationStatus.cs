namespace Jellyfin.Plugin.SendToKindle.WebIntegration;

public interface IWebIntegrationStatus
{
    bool IsRegistered { get; }

    string Message { get; }
}
