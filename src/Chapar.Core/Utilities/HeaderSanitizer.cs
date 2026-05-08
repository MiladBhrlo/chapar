namespace Chapar.Core.Utilities;

/// <summary>
/// Provides a helper to remove sensitive or dangerous data from message headers.
/// This is particularly useful before logging or persisting headers.
/// </summary>
public static class HeaderSanitizer
{
    // Headers that typically contain sensitive information and should never be logged or persisted as-is.
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "x-api-key",
        "token",
        "secret",
        "password",
        "credential",
        "cookie",
        "set-cookie"
    };

    // Characters that are commonly used in header injection attacks.
    private static readonly char[] DangerousChars = { '\r', '\n', '\0' };

    /// <summary>
    /// Sanitizes a dictionary of headers by:
    /// <list type="number">
    ///   <item>Replacing the value of any key in the <paramref name="sensitiveKeys"/> set with "[REDACTED]".</item>
    ///   <item>Removing carriage return, line feed, and null characters from all header values.</item>
    ///   <item>Optionally restricting headers to a pre‑defined set of allowed keys.</item>
    /// </list>
    /// The original dictionary is not modified.
    /// </summary>
    /// <param name="headers">The headers to sanitize. Can be null.</param>
    /// <param name="additionalSensitiveKeys">Additional sensitive keys to redact (on top of the built‑in set).</param>
    /// <param name="allowedKeys">
    /// If provided, only headers whose keys are in this set will be included in the result.
    /// If null or empty, all non‑redacted headers are kept.
    /// </param>
    /// <returns>A new, safe dictionary. Never returns null.</returns>
    public static IDictionary<string, object?> Sanitize(IDictionary<string, object?>? headers,
                                                        IEnumerable<string>? additionalSensitiveKeys = null,
                                                        IEnumerable<string>? allowedKeys = null)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (headers is null || headers.Count == 0)
            return result;

        var redactKeys = new HashSet<string>(SensitiveKeys, StringComparer.OrdinalIgnoreCase);
        if (additionalSensitiveKeys is not null)
        {
            foreach (var key in additionalSensitiveKeys)
                redactKeys.Add(key);
        }

        var allowed = allowedKeys is not null && allowedKeys.Any()
            ? new HashSet<string>(allowedKeys, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var kvp in headers)
        {
            // If allowedKeys is specified and this key is not in it, skip entirely.
            if (allowed is not null && !allowed.Contains(kvp.Key))
                continue;

            // Redact sensitive values.
            if (redactKeys.Contains(kvp.Key))
            {
                result[kvp.Key] = "[REDACTED]";
                continue;
            }

            // Remove dangerous characters from the value.
            var safeValue = kvp.Value?.ToString();
            if (safeValue is not null)
            {
                safeValue = new string(safeValue.Where(c => !DangerousChars.Contains(c)).ToArray());
            }

            result[kvp.Key] = safeValue;
        }

        return result;
    }
}
