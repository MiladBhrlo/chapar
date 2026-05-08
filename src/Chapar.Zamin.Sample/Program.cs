using Chapar.Zamin.MassTransit.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zamin.Core.Contracts.ApplicationServices.Events;
using Zamin.Extensions.DependencyInjection;
using Zamin.Extensions.MessageBus.Abstractions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddZaminNewtonSoftSerializer();
        services.AddChaparZaminMassTransit(opt =>
        {
            opt.Host = "localhost";
            opt.Username = "guest";
            opt.Password = "guest";
        });

        services.AddScoped<IEventDispatcher, FakeEventDispatcher>();
        services.AddSingleton<IMessageInboxItemRepository, FakeInboxRepo>();
    })
    .Build();

// Start the host to activate MassTransit bus and consumers
await host.StartAsync();

var sender = host.Services.GetRequiredService<ISendMessageBus>();
sender.Send(new Parcel
{
    MessageId = Guid.NewGuid().ToString(),
    MessageName = nameof(TestDomainEvent),
    MessageBody = "{}",
    Route = "TestApp.event.TestEvent",
    Headers = new Dictionary<string, object>()
});

Console.WriteLine("Parcel sent. Waiting...");
await Task.Delay(2000);
await host.StopAsync();
