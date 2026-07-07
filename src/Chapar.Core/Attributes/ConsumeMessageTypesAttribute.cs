namespace Chapar.Core.Attributes;

/// <summary>
/// Specifies additional broker-level message names that a handler should consume.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ConsumeMessageTypesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumeMessageTypesAttribute"/> class.
    /// </summary>
    /// <param name="messageTypes">The additional broker-level message names to bind.</param>
    public ConsumeMessageTypesAttribute(params string[] messageTypes)
    {
        MessageTypes = messageTypes ?? [];
    }

    /// <summary>
    /// Gets the additional broker-level message names.
    /// </summary>
    public IReadOnlyList<string> MessageTypes { get; }
}
