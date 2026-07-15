namespace Jellyfin.Plugin.SendToKindle.Configuration;

public enum SmtpSecurityMode
{
    Auto,
    StartTls,
    SslOnConnect,
    None,
}
