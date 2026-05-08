namespace Chapar.Core.Attributes;

/// <summary>
/// Specifies the expected origin service for a message handler.
/// When applied, the <c>OriginValidationBehaviour</c> in the pipeline checks
/// that the incoming message contains an "Origin" header with this value.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AllowedOriginAttribute : Attribute
{
    public string Origin { get; }

    public AllowedOriginAttribute(string origin) => Origin = origin;
}
