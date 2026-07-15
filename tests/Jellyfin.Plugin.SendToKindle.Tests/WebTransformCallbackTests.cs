using Jellyfin.Plugin.SendToKindle.WebIntegration;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class WebTransformCallbackTests
{
    [Fact]
    public void Transform_InsertsClientBeforeBodyEnd()
    {
        const string input = "<html><body><main>Jellyfin</main></body></html>";

        string result = WebTransformCallback.Transform(new WebTransformPayload { Contents = input });

        Assert.Contains("data-send-to-kindle-client", result, StringComparison.Ordinal);
        Assert.True(
            result.IndexOf("data-send-to-kindle-client", StringComparison.Ordinal)
            < result.IndexOf("</body>", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_DoesNotInjectTwice()
    {
        const string input = "<html><body></body></html>";

        string once = WebTransformCallback.Transform(new WebTransformPayload { Contents = input });
        string twice = WebTransformCallback.Transform(new WebTransformPayload { Contents = once });

        Assert.Equal(once, twice);
    }
}
