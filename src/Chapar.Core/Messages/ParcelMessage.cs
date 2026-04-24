namespace Chapar.Core.Messages;

/// <summary>
/// A Chapar event that carriers the parcel data from Third party's outbox.
/// </summary>
public class ParcelMessage : Abstractions.IEvent
{
    public string MessageId { get; init; } = string.Empty;
    public string MessageName { get; init; } = string.Empty;
    public string MessageBody { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public Dictionary<string, object> Headers { get; init; } = new();
}