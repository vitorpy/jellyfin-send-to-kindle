namespace Jellyfin.Plugin.SendToKindle.Jobs;

public enum SendJobStatus
{
    Queued,
    Converting,
    Sending,
    Succeeded,
    Failed,
}
