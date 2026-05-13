namespace Chapar.Core.Attributes;

/// <summary>
/// Specifies that a message should be published to a specific exchange,
/// or that a handler should consume from a specific exchange.
/// Can be applied multiple times for complex routing topologies.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ExchangeAttribute : Attribute
{
    /// <summary>The name of the exchange.</summary>
    public string Name { get; }

    /// <summary>
    /// The type of the exchange. Default is <see cref="Attributes.ExchangeType.Fanout"/>.
    /// </summary>
    public ExchangeType Type { get; init; } = ExchangeType.Fanout;

    /// <summary>The routing key used for direct or topic exchanges.</summary>
    public string? RoutingKey { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the exchange.</param>
    public ExchangeAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
