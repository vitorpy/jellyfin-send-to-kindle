using Jellyfin.Plugin.SendToKindle.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Jellyfin.Plugin.SendToKindle.Email;

public sealed class SmtpDeliveryService : ISmtpDeliveryService
{
    public const string PasswordEnvironmentVariable = "JELLYFIN_SEND_TO_KINDLE_SMTP_PASSWORD";

    private readonly IPluginConfigurationAccessor _configurationAccessor;

    public SmtpDeliveryService(IPluginConfigurationAccessor configurationAccessor)
    {
        _configurationAccessor = configurationAccessor;
    }

    public async Task SendAsync(
        IReadOnlyList<string> files,
        string title,
        Action<int, int> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(progress);
        if (files.Count == 0)
        {
            throw new ArgumentException("At least one converted file is required.", nameof(files));
        }

        PluginConfiguration configuration = _configurationAccessor.Current;
        Validate(configuration);

        using SmtpClient client = new();
        await ConnectAndAuthenticateAsync(client, configuration, cancellationToken).ConfigureAwait(false);

        int sent = 0;
        try
        {
            for (int index = 0; index < files.Count; index++)
            {
                MimeMessage message = CreateMessage(configuration, files[index], title, index, files.Count);
                await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
                sent++;
                progress(sent, files.Count);
            }
        }
        catch (Exception exception) when (sent > 0 && exception is not OperationCanceledException)
        {
            throw new PartialDeliveryException(
                $"SMTP delivery stopped after {sent} of {files.Count} parts.",
                sent,
                files.Count,
                exception);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(quit: true, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    public async Task CheckConnectionAsync(CancellationToken cancellationToken)
    {
        PluginConfiguration configuration = _configurationAccessor.Current;
        Validate(configuration);
        using SmtpClient client = new();
        await ConnectAndAuthenticateAsync(client, configuration, cancellationToken).ConfigureAwait(false);
        await client.NoOpAsync(cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
    }

    public static string ResolvePassword(PluginConfiguration configuration)
    {
        string? environmentPassword = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);
        return string.IsNullOrEmpty(environmentPassword) ? configuration.SmtpPassword : environmentPassword;
    }

    private static async Task ConnectAndAuthenticateAsync(
        SmtpClient client,
        PluginConfiguration configuration,
        CancellationToken cancellationToken)
    {
        await client.ConnectAsync(
            configuration.SmtpHost,
            configuration.SmtpPort,
            ToSocketOptions(configuration.SmtpSecurity),
            cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(configuration.SmtpUsername))
        {
            string password = ResolvePassword(configuration);
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException(
                    $"SMTP authentication requires a stored password or the {PasswordEnvironmentVariable} environment variable.");
            }

            await client.AuthenticateAsync(configuration.SmtpUsername, password, cancellationToken).ConfigureAwait(false);
        }
    }

    private static MimeMessage CreateMessage(
        PluginConfiguration configuration,
        string file,
        string title,
        int index,
        int total)
    {
        MimeMessage message = new();
        message.From.Add(MailboxAddress.Parse(configuration.SenderAddress));
        message.To.Add(MailboxAddress.Parse(configuration.KindleAddress));
        message.Subject = total == 1
            ? $"Send to Kindle: {title}"
            : $"Send to Kindle: {title} ({index + 1}/{total})";

        BodyBuilder body = new()
        {
            TextBody = total == 1
                ? $"Converted by Jellyfin Send to Kindle: {title}"
                : $"Converted by Jellyfin Send to Kindle: {title}, part {index + 1} of {total}",
        };
        body.Attachments.Add(file, ContentType.Parse("application/epub+zip"));
        message.Body = body.ToMessageBody();
        return message;
    }

    private static void Validate(PluginConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.SmtpHost))
        {
            throw new InvalidOperationException("The SMTP host is not configured.");
        }

        if (configuration.SmtpPort is < 1 or > 65535)
        {
            throw new InvalidOperationException("The SMTP port must be between 1 and 65535.");
        }

        if (!MailboxAddress.TryParse(configuration.SenderAddress, out _))
        {
            throw new InvalidOperationException("The SMTP sender address is invalid.");
        }

        if (!MailboxAddress.TryParse(configuration.KindleAddress, out _))
        {
            throw new InvalidOperationException("The Kindle recipient address is invalid.");
        }
    }

    private static SecureSocketOptions ToSocketOptions(SmtpSecurityMode mode)
    {
        return mode switch
        {
            SmtpSecurityMode.Auto => SecureSocketOptions.Auto,
            SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurityMode.None => SecureSocketOptions.None,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown SMTP security mode."),
        };
    }
}
