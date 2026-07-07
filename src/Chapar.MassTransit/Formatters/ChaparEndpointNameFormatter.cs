using MassTransit;

namespace Chapar.MassTransit.Formatters;

/// <summary>
/// Custom endpoint name formatter for deterministic Chapar queue naming.
/// </summary>
internal sealed class ChaparEndpointNameFormatter : DefaultEndpointNameFormatter
{
    private readonly string? _prefix;
    private readonly string? _suffix;

    public ChaparEndpointNameFormatter(string? prefix = null,
                                       string? suffix = null,
                                       bool includeNamespace = true)
        : base(includeNamespace)
    {
        _prefix = prefix;
        _suffix = suffix;
    }

    private string FormatName(string endpointName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpointName);
        return $"{_prefix}{endpointName}{_suffix}";
    }

    /// <inheritdoc />
    public override string Consumer<T>() => FormatName(base.Consumer<T>());

    /// <inheritdoc />
    public override string Message<T>() => FormatName(base.Message<T>());

    /// <inheritdoc />
    public override string TemporaryEndpoint(string tag) => FormatName(base.TemporaryEndpoint(tag));

    /// <inheritdoc />
    public override string Saga<T>() => FormatName(base.Saga<T>());
}
