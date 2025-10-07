using Digizuite.Common.Constants;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Digizuite.PressHandler;

class Program
{
    private static IConnection? _connection;
    private static IChannel? _channel;

    static async Task Main()
    {
        await SetupConnectionsAsync();

        if (_channel == null)
            return;

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var id = ea.BasicProperties.CorrelationId;
                var headers = new Dictionary<string, object?> { { "OrderId", 2 } };

                Console.WriteLine("[PressHandler] Consumed and handled: {0}", id);
                
                var properties = new BasicProperties()
                {
                    CorrelationId = id,
                    Headers = headers
                };

                await _channel.BasicPublishAsync(
                    exchange: Exchanges.HandledExchange,
                    routingKey: "press.handled.mp4",
                    body: ea.Body,
                    basicProperties: properties, 
                    mandatory: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[PressHandler] Error occured");
            }
        };

        await _channel.BasicConsumeAsync(Queues.Mp4PressQueue, true, consumer);

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

        for (int i = 0; i < 10; i++)
        {
            try
            {
                Console.Clear();

                await _channel.QueueDeclarePassiveAsync(Queues.Mp4PressQueue);

                Console.WriteLine("[PressHandler] Ready to consume Press Messages!");
                break;
            }
            catch (Exception)
            {
                Console.WriteLine($"[PressHandler] Waiting for queue... ({i + 1}/10)");

                await Task.Delay(2000);

                if (i != 9) continue;

                Console.WriteLine("[PressHandler] No queue found, Is Recipient running?");
                return;
            }
        }
    }
}