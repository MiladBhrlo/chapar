namespace Chapar.Core.Attributes;

/// <summary>
/// Overrides the logical message name used for broker-level contract identification.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class MessageNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The stable logical message name.</param>
    public MessageNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the stable logical message name.
    /// </summary>
    public string Name { get; }
}
