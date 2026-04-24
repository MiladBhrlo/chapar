using Chapar.Core.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Chapar.MassTransit.Filters;

/// <summary>
/// A MassTransit consume filter that adapts the technology‑agnostic <see cref="IConsumeFilter"/>
/// to MassTransit's pipeline.
/// </summary>
internal sealed class ChaparConsumeFilterAdapter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly IEnumerable<IConsumeFilter> _filters;
    private readonly ILogger<ChaparConsumeFilterAdapter<T>> _logger;

    public ChaparConsumeFilterAdapter(IEnumerable<IConsumeFilter> filters, ILogger<ChaparConsumeFilterAdapter<T>> logger)
    {
        _filters = filters;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageId = context.MessageId?.ToString() ?? Guid.NewGuid().ToString();
        var consumerName = typeof(T).FullName ?? typeof(T).Name;

        foreach (var filter in _filters)
        {
            if (!await filter.ShouldProcessAsync(messageId, consumerName))
            {
                _logger.LogInformation("Message {MessageId} was filtered out by {FilterType}.", messageId, filter.GetType().Name);
                return; // فیلتر تصمیم به رد کردن گرفت، پیام مصرف نمی‌شود
            }
        }

        try
        {
            await next.Send(context);

            // علامت‌گذاری پیام به‌عنوان پردازش‌شده توسط تمام فیلترها
            foreach (var filter in _filters)
            {
                await filter.MarkAsProcessedAsync(messageId, consumerName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consumer {ConsumerName} failed for message {MessageId}.", consumerName, messageId);
            throw;
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("ChaparConsumeFilter");
}