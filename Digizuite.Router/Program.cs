using Digizuite.Common.Constants;
using Digizuite.Common.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Digizuite.Router;

class Program
{
    private static IConnection? _connection;
    private static IChannel? _channel;
    
    static async Task Main()
    { 
        await SetupConnectionsAsync();
        await DeclareConnectionsAsync();

        if (_channel is null)
            return;
        
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var content = ea.BasicProperties.ContentType;

                switch (content)
                {
                    case "png":
                        await PublishMessageAsync(Exchanges.PngExchange, "new.png", ea.Body);
                        break;
                    case "pptx":
                        await PublishMessageAsync(Exchanges.PptxExchange, "new.pptx", ea.Body);
                        break;
                    case "mp4":
                        await PublishMessageAsync(Exchanges.Mp4Exchange, "new.mp4", ea.Body);
                        break;
                    case "image/jpeg":
                        await PublishMessageAsync(Exchanges.JpegExchange, "new.jpeg", ea.Body);
                        break;
                    default:
                        Console.WriteLine("Invalid format, send to invalid message");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed {0}", ex.Message);
            }
        };

        await _channel.BasicConsumeAsync(Queues.NewFileQueue, true, consumer);
        
        Console.ReadLine();
    }

    private static async Task PublishMessageAsync(string exchange, string routingKey, ReadOnlyMemory<byte> body)
    {
        if (_channel is null)
            return;

        Console.WriteLine($"Published message to {exchange}:{routingKey}");
        
        await _channel.BasicPublishAsync(exchange, routingKey, true, body);
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

    private static async Task DeclareConnectionsAsync()
    {
        if (_channel is null) return;

        await _channel.ExchangeDeclareAsync(Exchanges.FileExchange, ExchangeType.Direct, durable: false);
    
        await _channel.ExchangeDeclareAsync(Exchanges.Mp4Exchange, ExchangeType.Direct, durable: false);
        await _channel.ExchangeDeclareAsync(Exchanges.PngExchange, ExchangeType.Direct, durable: false);
        await _channel.ExchangeDeclareAsync(Exchanges.PptxExchange, ExchangeType.Direct, durable: false);
        await _channel.ExchangeDeclareAsync(Exchanges.JpegExchange, ExchangeType.Direct, durable: false);

        await _channel.QueueDeclareAsync(Queues.NewFileQueue, durable: false);
        await _channel.QueueBindAsync(Queues.NewFileQueue, Exchanges.FileExchange, "file.queue.new");
    }
}