using Chapar.Core.Attributes;

namespace Chapar.MassTransit.Formatters;

/// <summary>
/// Generates deterministic queue names for Chapar consumers.
/// </summary>
internal static class ChaparQueueNameFormatter
{
    public static string Format(Type handlerType,
                                string? prefix = null,
                                string? suffix = null)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        var explicitName = handlerType
            .GetCustomAttributes(typeof(QueueNameAttribute), false)
            .Cast<QueueNameAttribute>()
            .FirstOrDefault()
            ?.Name;

        var baseName = explicitName
                       ?? handlerType.FullName
                       ?? handlerType.Name;

        return $"{prefix}{baseName}{suffix}";
    }
}
