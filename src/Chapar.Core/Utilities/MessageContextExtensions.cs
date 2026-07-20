using Chapar.Core.Abstractions;

namespace Chapar.Core.Utilities;

/// <summary>
/// Provides strongly typed helper methods for reading values from message context headers.
/// </summary>
public static class MessageContextExtensions
{
    /// <summary>
    /// Attempts to retrieve a header value as a string.
    /// </summary>
    public static string? GetHeader(this IMessageContext context, string key)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);

        return context.Headers.TryGetValue(key, out var value)
            ? value?.ToString()
            : null;
    }

    /// <summary>
    /// Attempts to retrieve a header value as a <see cref="long"/>.
    /// </summary>
    public static long? GetInt64Header(this IMessageContext context, string key)
    {
        var raw = context.GetHeader(key);

        return long.TryParse(raw, out var value)
            ? value
            : null;
    }

    /// <summary>
    /// Attempts to retrieve a header value as a <see cref="Guid"/>.
    /// </summary>
    public static Guid? GetGuidHeader(this IMessageContext context, string key)
    {
        var raw = context.GetHeader(key);

        return Guid.TryParse(raw, out var value)
            ? value
            : null;
    }
}
