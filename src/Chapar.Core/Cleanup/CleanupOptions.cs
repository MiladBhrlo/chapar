namespace Chapar.Core.Cleanup;

/// <summary>
/// Configuration for the cleanup background service.
/// </summary>
public class CleanupOptions
{
    private TimeSpan _retentionPeriod = TimeSpan.FromDays(7);
    private TimeSpan _interval = TimeSpan.FromHours(1);

    /// <summary>Whether the cleanup job is enabled. Default is <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Records older than this time span will be deleted. Default is 7 days.</summary>
    public TimeSpan RetentionPeriod
    {
        get => _retentionPeriod;
        set
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), value, "RetentionPeriod must be greater than zero.");
            _retentionPeriod = value;
        }
    }

    /// <summary>Interval at which the cleanup runs. Default is 1 hour.</summary>
    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Interval must be greater than zero.");
            _interval = value;
        }
    }
}
