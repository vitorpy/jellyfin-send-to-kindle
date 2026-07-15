using Jellyfin.Plugin.SendToKindle.Configuration;
using Jellyfin.Plugin.SendToKindle.Conversion;
using Jellyfin.Plugin.SendToKindle.Email;
using Jellyfin.Plugin.SendToKindle.Jobs;
using Jellyfin.Plugin.SendToKindle.WebIntegration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.SendToKindle;

public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IPluginConfigurationAccessor, PluginConfigurationAccessor>();
        serviceCollection.AddSingleton<IProcessRunner, ProcessRunner>();
        serviceCollection.AddSingleton<AdvancedArgumentParser>();
        serviceCollection.AddSingleton<KccArgumentBuilder>();
        serviceCollection.AddSingleton<IBookConversionService, BookConversionService>();
        serviceCollection.AddSingleton<ISmtpDeliveryService, SmtpDeliveryService>();

        serviceCollection.AddSingleton<SendJobQueue>();
        serviceCollection.AddSingleton<ISendJobQueue>(provider => provider.GetRequiredService<SendJobQueue>());
        serviceCollection.AddSingleton<IHostedService>(provider => provider.GetRequiredService<SendJobQueue>());

        serviceCollection.AddSingleton<WebIntegrationStatus>();
        serviceCollection.AddSingleton<IWebIntegrationStatus>(provider => provider.GetRequiredService<WebIntegrationStatus>());
        serviceCollection.AddHostedService<FileTransformationRegistrationService>();
    }
}
