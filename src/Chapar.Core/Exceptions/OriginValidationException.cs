namespace Chapar.Core.Exceptions;

/// <summary>
/// The exception that is thrown when the origin of an incoming message does not match
/// the expected value defined by <see cref="Attributes.AllowedOriginAttribute"/>.
/// Implements <see cref="IDomainException"/> so that the pipeline does not trigger retries.
/// </summary>
public class OriginValidationException : InvalidOperationException, IDomainException
{
    public string ExpectedOrigin { get; }
    public string? ActualOrigin { get; }

    public OriginValidationException(string expectedOrigin, string? actualOrigin)
        : base($"Message origin validation failed. Expected '{expectedOrigin}', but received '{actualOrigin ?? "null"}'.")
    {
        ExpectedOrigin = expectedOrigin;
        ActualOrigin = actualOrigin;
    }
}