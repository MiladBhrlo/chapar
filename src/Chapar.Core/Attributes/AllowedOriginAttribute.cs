namespace Chapar.Core.Attributes;

/// <summary>
/// Specifies the expected origin service name for a message handler.
/// When applied, the <see cref="Pipeline.Behaviours.OriginValidationBehaviour{T}"/>
/// checks that the incoming message contains an "X-Origin-Service" header with this value.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AllowedOriginAttribute : Attribute
{
    public string Origin { get; }

    public AllowedOriginAttribute(string origin) => Origin = origin;
}