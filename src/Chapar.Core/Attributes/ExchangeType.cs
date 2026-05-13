namespace Chapar.Core.Attributes;

/// <summary>
/// Represents the type of an exchange in RabbitMQ.
/// </summary>
public enum ExchangeType
{
    /// <summary>Broadcasts messages to all bound queues.</summary>
    Fanout,

    /// <summary>Routes messages to a specific queue based on a routing key.</summary>
    Direct,

    /// <summary>Routes messages based on a pattern matching the routing key.</summary>
    Topic,

    /// <summary>Routes messages based on header values instead of routing keys.</summary>
    Headers
}
