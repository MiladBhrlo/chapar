using Chapar.Core.Attributes;

namespace Chapar.MassTransit.Options;

/// <summary>
/// Configuration for a default exchange binding.
/// </summary>
public class ExchangeConfig
{
    /// <summary>The name of the exchange.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of the exchange. Default is <see cref="ExchangeType"/>.
    /// </summary>
    public ExchangeType Type { get; init; } = ExchangeType.Fanout;

    /// <summary>The routing key used for direct or topic exchanges.</summary>
    public string? RoutingKey { get; set; }
}
