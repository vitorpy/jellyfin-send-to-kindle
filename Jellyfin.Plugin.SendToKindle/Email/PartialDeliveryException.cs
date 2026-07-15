namespace Jellyfin.Plugin.SendToKindle.Email;

public sealed class PartialDeliveryException : Exception
{
    public PartialDeliveryException(string message, int partsSent, int totalParts, Exception innerException)
        : base(message, innerException)
    {
        PartsSent = partsSent;
        TotalParts = totalParts;
    }

    public int PartsSent { get; }

    public int TotalParts { get; }
}
