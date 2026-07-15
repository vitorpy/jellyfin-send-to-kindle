namespace Jellyfin.Plugin.SendToKindle.Email;

public interface ISmtpDeliveryService
{
    Task SendAsync(
        IReadOnlyList<string> files,
        string title,
        Action<int, int> progress,
        CancellationToken cancellationToken);

    Task CheckConnectionAsync(CancellationToken cancellationToken);
}
