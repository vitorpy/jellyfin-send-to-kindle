using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.SendToKindle.WebIntegration;

public sealed class FileTransformationRegistrationService : IHostedService
{
    public static readonly Guid TransformationId = Guid.Parse("5f0a3d1d-f41e-4a8f-a5a0-126adbb09a95");

    private readonly WebIntegrationStatus _status;
    private readonly ILogger<FileTransformationRegistrationService> _logger;

    public FileTransformationRegistrationService(
        WebIntegrationStatus status,
        ILogger<FileTransformationRegistrationService> logger)
    {
        _status = status;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            Assembly? assembly = AssemblyLoadContext.All
                .SelectMany(context => context.Assemblies)
                .FirstOrDefault(candidate => string.Equals(
                    candidate.GetName().Name,
                    "Jellyfin.Plugin.FileTransformation",
                    StringComparison.Ordinal));
            Type? pluginInterface = assembly?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            MethodInfo? registerMethod = pluginInterface?.GetMethod(
                "RegisterTransformation",
                BindingFlags.Public | BindingFlags.Static);
            if (registerMethod is null)
            {
                const string message = "File Transformation is not installed; the book-detail action is unavailable.";
                _status.Set(false, message);
                _logger.LogWarning("{Message}", message);
                return Task.CompletedTask;
            }

            Type callbackType = typeof(WebTransformCallback);
            JObject payload = JObject.FromObject(new
            {
                id = TransformationId,
                fileNamePattern = "(^|/)index\\.html$",
                callbackAssembly = callbackType.Assembly.FullName,
                callbackClass = callbackType.FullName,
                callbackMethod = nameof(WebTransformCallback.Transform),
            });
            registerMethod.Invoke(null, new object?[] { payload });

            const string success = "File Transformation registered the book-detail action.";
            _status.Set(true, success);
            _logger.LogInformation("{Message}", success);
        }
        catch (Exception exception)
        {
            const string message = "File Transformation registration failed; the book-detail action is unavailable.";
            _status.Set(false, $"{message} {exception.GetBaseException().Message}");
            _logger.LogError(exception, "{Message}", message);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
