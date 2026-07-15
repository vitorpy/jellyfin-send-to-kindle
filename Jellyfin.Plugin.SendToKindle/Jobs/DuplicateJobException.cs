namespace Jellyfin.Plugin.SendToKindle.Jobs;

public sealed class DuplicateJobException : Exception
{
    public DuplicateJobException(SendJobSnapshot existingJob)
        : base($"A send job for this item is already {existingJob.Status.ToString().ToLowerInvariant()}.")
    {
        ExistingJob = existingJob;
    }

    public SendJobSnapshot ExistingJob { get; }
}
