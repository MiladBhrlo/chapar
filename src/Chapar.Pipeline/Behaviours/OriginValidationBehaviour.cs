using Chapar.Core.Abstractions;
using Chapar.Core.Attributes;
using Chapar.Core.Exceptions;
using Chapar.Core.Pipeline;
using Chapar.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Chapar.Pipeline.Behaviours;

/// <summary>
/// Validates the origin of an incoming message by checking a configurable header
/// against the <see cref="AllowedOriginAttribute"/> applied on the message type.
/// This behavior is automatically registered when <c>AddChaparPipeline</c> is called.
/// </summary>
/// <typeparam name="TMessage">The type of the message being handled.</typeparam>
public sealed class OriginValidationBehaviour<TMessage> : IPipelineBehavior<TMessage>
    where TMessage : IMessage
{
    private readonly IMessageContextAccessor? _contextAccessor;
    private readonly ILogger<OriginValidationBehaviour<TMessage>> _logger;
    private readonly string _headerName;

    // Cache the attribute per message type to avoid reflection on every invocation.
    private static readonly AllowedOriginAttribute? _CachedAttribute =
        Attribute.GetCustomAttribute(typeof(TMessage), typeof(AllowedOriginAttribute)) as AllowedOriginAttribute;

    /// <summary>
    /// Initializes a new instance of the <see cref="OriginValidationBehaviour{TMessage}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="contextAccessor">An optional accessor to read message headers.</param>
    /// <param name="headerName">The header key to check for origin. Default is "Origin".</param>
    public OriginValidationBehaviour(ILogger<OriginValidationBehaviour<TMessage>> logger,
                                     IMessageContextAccessor? contextAccessor = null,
                                     string headerName = "Origin")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextAccessor = contextAccessor;
        _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));
    }

    /// <inheritdoc />
    public async Task HandleAsync(TMessage message,
                                  Func<Task> next,
                                  CancellationToken cancellationToken)
    {
        if (_CachedAttribute is null)
        {
            // No attribute → nothing to validate
            await next();
            return;
        }

        // Fail closed: if attribute is present but headers are unavailable, reject the message.
        if (_contextAccessor?.Headers is not { } headers)
        {
            _logger.LogError(
                "Origin validation failed for {MessageType}. [AllowedOrigin] is present but message headers are unavailable.",
                typeof(TMessage).Name);

            throw new OriginValidationException(_CachedAttribute.Origin, null);
        }

        var rawOrigin = headers.TryGetValue(_headerName, out var value) ? value?.ToString() : null;
        var actualOrigin = new string(rawOrigin?.Where(c => !HeaderSanitizer.DangerousChars.Contains(c)).ToArray() ?? []);

        if (!string.Equals(_CachedAttribute.Origin, actualOrigin, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Origin validation failed for {MessageType}. Expected '{Expected}', got '{Actual}'.",
                typeof(TMessage).Name, _CachedAttribute.Origin, actualOrigin);

            throw new OriginValidationException(_CachedAttribute.Origin, actualOrigin);
        }

        _logger.LogDebug("Origin validated for {MessageType}: {Origin}.", typeof(TMessage).Name, actualOrigin);
        await next();
    }
}
