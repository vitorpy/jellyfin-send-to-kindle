namespace Jellyfin.Plugin.SendToKindle.Jobs;

public interface ISendJobQueue
{
    SendJobSnapshot Enqueue(BookSource source);

    SendJobSnapshot? Get(Guid jobId);

    IReadOnlyList<SendJobSnapshot> GetRecent(int limit);
}
