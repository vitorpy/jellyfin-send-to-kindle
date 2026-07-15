namespace Jellyfin.Plugin.SendToKindle.WebIntegration;

public sealed class WebIntegrationStatus : IWebIntegrationStatus
{
    private string _message = "File Transformation registration has not run.";
    private int _registered;

    public bool IsRegistered => Volatile.Read(ref _registered) == 1;

    public string Message => Volatile.Read(ref _message);

    public void Set(bool registered, string message)
    {
        Volatile.Write(ref _message, message);
        Volatile.Write(ref _registered, registered ? 1 : 0);
    }
}
