using Jellyfin.Plugin.SendToKindle.Conversion;
using Jellyfin.Plugin.SendToKindle.Email;
using Jellyfin.Plugin.SendToKindle.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class SendJobQueueTests
{
    [Fact]
    public void Enqueue_RejectsDuplicateActiveItem()
    {
        SendJobQueue queue = new(
            new UnusedConversionService(),
            new UnusedDeliveryService(),
            NullLogger<SendJobQueue>.Instance);
        BookSource source = new(Guid.NewGuid(), "/library/book.epub", "Book", "Author");

        SendJobSnapshot first = queue.Enqueue(source);
        DuplicateJobException exception = Assert.Throws<DuplicateJobException>(() => queue.Enqueue(source));

        Assert.Equal(first.JobId, exception.ExistingJob.JobId);
    }

    private sealed class UnusedConversionService : IBookConversionService
    {
        public Task<ConversionResult> ConvertAsync(BookSource source, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class UnusedDeliveryService : ISmtpDeliveryService
    {
        public Task SendAsync(
            IReadOnlyList<string> files,
            string title,
            Action<int, int> progress,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CheckConnectionAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
