using System.Reflection;
using Chapar.Core.Attributes;
using Chapar.MassTransit.Options;
using MassTransit;

namespace Chapar.MassTransit.Formatters;

/// <summary>
/// Generates stable and deterministic broker entity names for published messages.
/// </summary>
internal sealed class ChaparMessageNameFormatter : IEntityNameFormatter
{
    private readonly ChaparMassTransitOptions _options;

    public ChaparMessageNameFormatter(ChaparMassTransitOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string FormatEntityName<T>()
    {
        var type = typeof(T);
        var attribute = type.GetCustomAttribute<MessageNameAttribute>();
        if (attribute is not null)
            return ApplyAffixes(attribute.Name);

        var fullName = type.FullName;
        if (fullName is not null &&
            _options.MessageTypeMappings.TryGetValue(fullName, out var mapped))
        {
            return ApplyAffixes(mapped);
        }

        return ApplyAffixes(type.FullName ?? type.Name);
    }

    private string ApplyAffixes(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return $"{_options.ExchangeNamePrefix}{name}{_options.ExchangeNameSuffix}";
    }
}
