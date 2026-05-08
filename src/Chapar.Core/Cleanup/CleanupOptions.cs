namespace Chapar.Core.Cleanup;

/// <summary>
/// Configuration for the cleanup background service.
/// </summary>
public class CleanupOptions
{
    /// <summary>Whether the cleanup job is enabled. Default is <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Records older than this time span will be deleted. Default is 7 days.</summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Interval at which the cleanup runs. Default is 1 hour.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}
