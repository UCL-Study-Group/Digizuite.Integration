using Digizuite.Common.Constants;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Digizuite.Resequencer;

class Program
{
    private static IConnection? _connection;
    private static IChannel? _channel;
    
    static async Task Main()
    {
        await SetupConnectionsAsync();
        await DeclareAsync();

        if (_channel is null)
            return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        var originalConsumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var routingKey = ea.RoutingKey;
                
                var orderId = ea.BasicProperties.Headers?["OrderId"] ?? "None";
                
                Console.WriteLine("[Resequencer] Received Handled from {0} with orderId {1}", routingKey, orderId);
            }
            catch (Exception)
            {
                Console.WriteLine("[Resequencer] Failed to consume message");
            }
        };

        originalConsumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                Console.WriteLine("[Resequencer] Received Original");
            }
            catch (Exception)
            {
                Console.WriteLine("[Resequencer] Failed to consume message");
            }
        };
        

        await _channel.BasicConsumeAsync(Queues.HandledQueue, true, consumer);
        await _channel.BasicConsumeAsync(Queues.Mp4OriginalQueue, true, originalConsumer);
        
        Console.WriteLine("[Resequencer] Ready to receive messages");
        Console.ReadLine();
    }
    
    private static async Task SetupConnectionsAsync()
    {
        if (_connection is not null && _connection.IsOpen)
            return;

        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        _connection = await factory.CreateConnectionAsync();
        
        _channel = await _connection.CreateChannelAsync();
    }

    private static async Task DeclareAsync()
    {
        if (_channel is null) return;

        await _channel.ExchangeDeclareAsync(Exchanges.HandledExchange, ExchangeType.Direct, false, true);

        await _channel.QueueDeclareAsync(Queues.HandledQueue, false, false, true);
        await _channel.QueueBindAsync(Queues.HandledQueue, Exchanges.HandledExchange, "press.handled.mp4");
        await _channel.QueueBindAsync(Queues.HandledQueue, Exchanges.HandledExchange, "web.handled.mp4");
    }
}