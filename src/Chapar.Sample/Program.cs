using Chapar.Core.Abstractions;
using Chapar.Core.Attributes;
using Chapar.MassTransit.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chapar.Sample;

// Messages
[MessageName("sample.order-placed.v1")]
public record OrderPlaced(Guid OrderId) : IEvent;
public record SendSms(string PhoneNumber, string Text) : ICommand;

// Handlers
public class OrderPlacedHandler : IMessageHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"📧 Order {message.OrderId} placed. Sending confirmations...");
        return Task.CompletedTask;
    }
}

[QueueName("sms-service")]
public class SendSmsHandler : IMessageHandler<SendSms>
{
    public Task HandleAsync(SendSms message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"📱 SMS to {message.PhoneNumber}: {message.Text}");
        return Task.CompletedTask;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                // Register Chapar with MassTransit
                services.AddChaparMassTransit(opt =>
                {
                    opt.Host = "localhost";
                    opt.Username = "guest";
                    opt.Password = "guest";
                    opt.DefaultHeaders["X-Service"] = "chapar-sample";
                    opt.Resilience.UseDeadLetter = true;
                    opt.Resilience.MaxRedelivery = 5;
                });
            })
            .Build();

        await host.StartAsync();

        var bus = host.Services.GetRequiredService<IChaparBus>();

        // Publish an event
        await bus.PublishAsync(
            new OrderPlaced(Guid.NewGuid()),
            new Dictionary<string, object> { ["Origin"] = "Chapar.Sample" });

        // Send a command to a specific queue (an imaginary sms-service)
        await bus.SendAsync(new SendSms("09121234567", "Your order has been placed."), "sms-service");

        Console.WriteLine("Messages sent. Press any key to exit...");
        Console.ReadKey();

        await host.StopAsync();
    }
}
