namespace Chapar.Core.Attributes;

/// <summary>
/// Specifies the queue name that a consumer should bind to when processing commands.
/// If not provided, MassTransit's default naming convention is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class QueueNameAttribute : Attribute
{
    public string Name { get; }

    public QueueNameAttribute(string name) => Name = name;
}