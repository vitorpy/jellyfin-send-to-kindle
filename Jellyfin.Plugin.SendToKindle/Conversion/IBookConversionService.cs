using Jellyfin.Plugin.SendToKindle.Jobs;

namespace Jellyfin.Plugin.SendToKindle.Conversion;

public interface IBookConversionService
{
    Task<ConversionResult> ConvertAsync(BookSource source, CancellationToken cancellationToken);
}
